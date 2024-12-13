import numpy as np
import tensorflow as tf
import random
from collections import deque
import socket
import csv


# 서버 정보 설정
HOST = '0.0.0.0'  # 로컬호스트 (Unity와 같은 PC에서 실행될 경우)
PORT = 65432        # 사용할 포트 번호

# 서버 소켓 생성
server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
server_socket.bind((HOST, PORT))
server_socket.listen(1)  # 최대 1개의 연결 대기

print(f"서버가 {HOST}:{PORT}에서 대기 중...")

# 클라이언트 연결 수락
conn, addr = server_socket.accept()
print(f"연결된 클라이언트 주소: {addr}")



# 하이퍼파라미터 설정
state_size = 2   # 입력은 2개의 좌표
action_size = 4  # 액션의 개수
batch_size = 32  # 배치 크기
gamma = 0.99     # 할인 계수
epsilon = 1.0    # 탐색을 위한 초기 탐사 비율
epsilon_min = 0.01
epsilon_decay = 0.995
learning_rate = 0.001
memory_size = 10000  # 경험 리플레이 크기
train_start = 1000   # 학습을 시작하기 위한 최소 경험 수

# Q-네트워크 모델 정의
class DQNModel(tf.keras.Model):
    def __init__(self):
        super(DQNModel, self).__init__()
        self.dense1 = tf.keras.layers.Dense(64, activation='relu', input_shape=(state_size,))
        self.dense2 = tf.keras.layers.Dense(64, activation='relu')
        self.output_layer = tf.keras.layers.Dense(action_size, activation='linear')  # Q-value output
    
    def call(self, state):
        x = self.dense1(state)
        x = self.dense2(x)
        return self.output_layer(x)

# 경험 리플레이 정의
class ReplayBuffer:
    def __init__(self, max_size):
        self.buffer = deque(maxlen=max_size)
    
    def add(self, experience):
        self.buffer.append(experience)
    
    def sample(self, batch_size):
        return random.sample(self.buffer, batch_size)
    
    def size(self):
        return len(self.buffer)

# DQN 에이전트 정의
class DQNAgent:
    def __init__(self):
        self.model = DQNModel()
        self.target_model = DQNModel()
        self.target_model.set_weights(self.model.get_weights())  # 타겟 모델 초기화
        self.optimizer = tf.keras.optimizers.Adam(learning_rate)
        self.memory = ReplayBuffer(memory_size)
        self.epsilon = epsilon

    def act(self, state):
        if np.random.rand() <= self.epsilon:
            return random.randrange(action_size)  # 랜덤 액션 선택 (탐사)
        q_values = self.model(np.array([state]))
        return np.argmax(q_values)  # 최대 Q-value를 가진 액션 선택 (활용)
    
    def learn(self):
        if self.memory.size() < train_start:
            return
        
        batch = self.memory.sample(batch_size)
        
        states = np.array([b[0] for b in batch])
        actions = np.array([b[1] for b in batch])
        rewards = np.array([b[2] for b in batch])
        next_states = np.array([b[3] for b in batch])
        dones = np.array([b[4] for b in batch])
        
        with tf.GradientTape() as tape:
            q_values = self.model(states)
            next_q_values = self.target_model(next_states)
            
            # 벨만 최적 방정식
            target = rewards + gamma * np.max(next_q_values, axis=1) * (1 - dones)
            one_hot_actions = tf.one_hot(actions, action_size)
            q_value = tf.reduce_sum(q_values * one_hot_actions, axis=1)
            
            loss = tf.reduce_mean(tf.square(target - q_value))  # MSE Loss

        grads = tape.gradient(loss, self.model.trainable_variables)
        self.optimizer.apply_gradients(zip(grads, self.model.trainable_variables))
        
        # 타겟 모델 업데이트
        if self.epsilon > epsilon_min:
            self.epsilon *= epsilon_decay
        
        # 일정 주기마다 타겟 네트워크를 업데이트
        self.target_model.set_weights(self.model.get_weights())

        return loss

# DQN 훈련 루프
def train():
    bestloss = 1.0

    agent = DQNAgent()
    episodes = 5000  # 학습할 에피소드 수  123,456
    for e in range(episodes):
        conn.sendall("NewStatus\n".encode())
        data = conn.recv(1024)  # 1024 바이트 크기
        print(data)
        state = data.decode().split(',')  # 통신으로 받아와야 함.
        state = [float(state[0]), float(state[1])]
        total_reward = 0
        done = False
        
        while not done:
            action = agent.act(state)  # 액션 선택
            #통신으로 액션 전달
            strAction = "{0}\n".format(action)
            conn.sendall(strAction.encode())
            #next_state, reward 받아야 함.
            data = conn.recv(1024)  # 1024 바이트 크기
            data = data.decode().split(',')  # 통신으로 받아와야 함.
            print(data)
            next_state = [float(data[0]), float(data[1])]
            reward = float(data[2])
            done = data[3].replace("\r\n", "")
            if done == "0":
                done = False
            else:
                done = True
            agent.memory.add((state, action, reward, next_state, done))  # 경험 리플레이에 저장
            state = next_state
            total_reward += float(reward)
            
            loss = agent.learn()  # 모델 학습
            print("loss =",loss)
            #conn.sendall("Status\n".encode())
            #data = conn.recv(1024)  # 1024 바이트 크기
            #state = data.decode().split(',')  # 통신으로 받아와야 함.
        
        if loss is not None and not tf.math.is_nan(loss) and loss < bestloss:
            bestloss = loss
            with open("training_best_log.csv", "a", newline="") as file:
                writer = csv.writer(file)
                writer.writerow([e + 1, loss, total_reward, agent.epsilon])
            agent.model.save('dqn_model_best', save_format='tf')

        with open("training_all_log.csv", "a", newline="") as file:
            writer = csv.writer(file)
            writer.writerow([e + 1, loss, total_reward, agent.epsilon])
        print(f"Episode: {e+1}/{episodes}, Total Reward: {total_reward}, Epsilon: {agent.epsilon}")
    
    agent.model.save('dqn_model', save_format='tf')

# 훈련 실행
with open("training_best_log.csv", "w", newline="") as file:
        writer = csv.writer(file)
        writer.writerow(["Episode", "Loss", "Total Reward", "Epsilon"])  # 헤더 추가

with open("training_all_log.csv", "w", newline="") as file:
        writer = csv.writer(file)
        writer.writerow(["Episode", "Loss", "Total Reward", "Epsilon"])  # 헤더 추가
train()
