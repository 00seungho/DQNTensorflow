import numpy as np
import tensorflow as tf
import random
from collections import deque
from tensorflow.keras.models import load_model
import socket
import csv
file_name = 'validation(lastmodel).csv'
with open(file_name, mode='w', newline='') as file:
    writer = csv.writer(file)
    writer.writerow(['Prediction Count', 'Clear Status', 'total_reward'])

action_size = 4  # 액션의 개수
state_size = 2   # 입력은 2개의 좌표
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

    
class DQNAgent:
    def __init__(self):
        self.model = load_model('dqn_model_best_패널티 적용 전')
        self.epsilon = 0.0
    def act(self, state):
        if np.random.rand() <= self.epsilon:
            return random.randrange(action_size)  # 랜덤 액션 선택 (탐사)
        q_values = self.model(np.array([state]))
        return np.argmax(q_values)  # 최대 Q-value를 가진 액션 선택 (활용)
    

agent = DQNAgent()
episodes = 500  # 학습할 에피소드 수  123,456

for e in range(episodes):
        conn.sendall("NewStatus\n".encode())
        data = conn.recv(1024)  # 1024 바이트 크기
        state = data.decode().split(',')  # 통신으로 받아와야 함.
        state = [float(state[0]), float(state[1])]
        total_reward = 0
        done = False
        prediction_count = 0  # 예측 횟수 초기화
        clear = False


        while not done:
            action = agent.act(state)  # 액션 선택
            prediction_count += 1  # 예측 횟수 증가
            #통신으로 액션 전달
            strAction = "{0}\n".format(action)
            conn.sendall(strAction.encode())
            #next_state, reward 받아야 함.
            data = conn.recv(1024)  # 1024 바이트 크기
            data = data.decode().split(',')  # 통신으로 받아와야 함.
            next_state = [float(data[0]), float(data[1])]
            reward = float(data[2])
            done = data[3].replace("\r\n", "")
            if done == "0":
                done = False
            else:
                done = True
            
            state = next_state
            total_reward += float(reward)
            if reward >= 1:
                clear = True
        with open(file_name, mode='a', newline='') as file:
            writer = csv.writer(file)
            writer.writerow([prediction_count, clear, total_reward])