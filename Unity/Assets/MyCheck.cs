//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Runtime.CompilerServices;
//using UnityEngine;
//using System.Net.Sockets;
//using System.Text;
//using System.IO;

//public class MyCheck : MonoBehaviour
//{
//    public GameObject agent;

//    private TcpClient tcpClient;
//    private NetworkStream networkStream;
//    private StreamReader reader;
//    private StreamWriter writer;

//    public string serverIP = "127.0.0.1";  // 서버 IP (localhost)
//    public int serverPort = 65432;         // 서버 포트

//    private Vector3 initPoint;
//    private int step = 0;
//    private int done = 0;

//    private void OnCollisionEnter(Collision collision)
//    {
//        if(collision.gameObject.name == "Sphere")
//        {
//            done = 1;
//        }

//    }


//    // Start is called before the first frame update
//    void Start()
//    {
//        ConnectToServer();
//        initPoint = transform.position - agent.transform.position;

//    }

//    void ConnectToServer()
//    {
//        try
//        {
//            tcpClient = new TcpClient(serverIP, serverPort);  // 서버에 연결
//            networkStream = tcpClient.GetStream();
//            reader = new StreamReader(networkStream);
//            writer = new StreamWriter(networkStream);
//            UnityEngine.Debug.Log("서버에 연결되었습니다.");
//        }
//        catch (SocketException e)
//        {
//            UnityEngine.Debug.LogError("서버 연결 실패: " + e.Message);
//        }
//    }

//    // 서버에 메시지를 보내는 메서드
//    void SendMessageToServer(string message)
//    {
//        if (tcpClient == null || !tcpClient.Connected) return;

//        writer.WriteLine(message);    // 메시지 작성
//        writer.Flush();                // 전송
//    }

//    // 연결 종료 처리
//    void OnApplicationQuit()
//    {
//        if (tcpClient != null)
//        {
//            reader.Close();
//            writer.Close();
//            networkStream.Close();
//            tcpClient.Close();
//        }
//    }

//    int ProState = -1;
//    void FixedUpdate()
//    {
//        var state = transform.position - agent.transform.position;
//        UnityEngine.Debug.Log(string.Format("{0:F2},{1:F2},{2:F2}", state.x, state.y, state.z));

//        //REQ STATE
//        string response = reader.ReadLine();
//        UnityEngine.Debug.Log("서버로부터 받은 응답: " + response);

//        if (response == "NewStatus")
//        {
//            ProState = 0;
//            SendMessageToServer(string.Format("{0:F2},{1:F2}", state.x, state.z));
//        }
//        else if(response == "Status")
//        {
//            //SEND STATE

//        }
//        else
//        {
//            switch (response)
//            {
//                case "0":
//                    agent.transform.position += Vector3.forward * 0.125f;
//                    break;
//                case "1":
//                    agent.transform.position += Vector3.back * 0.125f;
//                    break;
//                case "2":
//                    agent.transform.position += Vector3.left * 0.125f;
//                    break;
//                case "3":
//                    agent.transform.position += Vector3.right * 0.125f;
//                    break;
//            }
//            //next state
//            state = transform.position - agent.transform.position;
//            float reward = (Vector3.Magnitude(initPoint) - Vector3.Magnitude(state)) / Vector3.Magnitude(initPoint);
//            if (done == 1)
//            {
//                reward = 100;
//            }

//            reward += -0.01f * step;

//            if (step > 200)
//            {
//                done = 1;
//                reward = -100;
//            }
//            SendMessageToServer(string.Format("{0:F2},{1:F2},{2:F2},{3}", state.x, state.z, reward, done));
//        }

//        if (done == 1)
//        {
//            done = 0;
//            step = 0;

//            Rigidbody target = agent.gameObject.GetComponent<Rigidbody>();
//            Vector3 zero = new Vector3(0, 0, 0);
//            target.velocity = zero;
//            target.angularVelocity = zero;
//            agent.gameObject.transform.position = new Vector3(0, 0.5f, -3.5f);

//            System.Random rnd = new System.Random();
//            float rndX = rnd.Next(-35, 35) / 10.0f;
//            float rndZ = rnd.Next(5, 35) / 10.0f;

//            Rigidbody target2 = gameObject.GetComponent<Rigidbody>();
//            target2.velocity = zero;
//            target2.angularVelocity = zero;
//            transform.position = new Vector3(rndX, 0.5f, rndZ);

//            initPoint = transform.position - agent.transform.position;
//        }
//        step++;
//    }

//    // Update is called once per frame
//    void Update()
//    {

//    }
//}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;
using System.Net.Sockets;
using System.Text;
using System.IO;

public class MyCheck : MonoBehaviour
{
    public GameObject agent;

    private TcpClient tcpClient;
    private NetworkStream networkStream;
    private StreamReader reader;
    private StreamWriter writer;

    public string serverIP = "127.0.0.1";  // 서버 IP (localhost)
    public int serverPort = 65432;         // 서버 포트

    private Vector3 initPoint;
    private int step = 0;
    private int done = 0;

    private List<Vector3> positions; // 🔥 랜덤 위치 목록
    private int currentIndex = 0;    // 🔥 현재 위치 인덱스

    private void LoadCSV()
    {
        positions = new List<Vector3>();

        // 📁 Assets/Resources 폴더에 위치한 "random_positions.csv" 파일을 불러옵니다.
        TextAsset csvData = Resources.Load<TextAsset>("Random_Positions");

        // CSV 파일의 각 줄을 가져옵니다.
        string[] rows = csvData.text.Split('\n');

        // 첫 번째 줄(헤더)을 제외하고 나머지를 읽어들입니다.
        for (int i = 1; i < rows.Length; i++)
        {
            if (!string.IsNullOrEmpty(rows[i]))
            {
                string[] values = rows[i].Split(',');

                // X, Z 위치값을 float로 변환합니다.
                float x = float.Parse(values[0]);
                float z = float.Parse(values[1]);

                // Y 값은 0.5로 고정
                positions.Add(new Vector3(x, 0.5f, z));
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.name == "Sphere")
        {
            done = 1;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        LoadCSV(); // 🔥 CSV 파일 불러오기
        ConnectToServer();
        initPoint = transform.position - agent.transform.position;
    }

    void ConnectToServer()
    {
        try
        {
            tcpClient = new TcpClient(serverIP, serverPort);  // 서버에 연결
            networkStream = tcpClient.GetStream();
            reader = new StreamReader(networkStream);
            writer = new StreamWriter(networkStream);
            UnityEngine.Debug.Log("서버에 연결되었습니다.");
        }
        catch (SocketException e)
        {
            UnityEngine.Debug.LogError("서버 연결 실패: " + e.Message);
        }
    }

    // 서버에 메시지를 보내는 메서드
    void SendMessageToServer(string message)
    {
        if (tcpClient == null || !tcpClient.Connected) return;

        writer.WriteLine(message);    // 메시지 작성
        writer.Flush();                // 전송
    }

    // 연결 종료 처리
    void OnApplicationQuit()
    {
        if (tcpClient != null)
        {
            reader.Close();
            writer.Close();
            networkStream.Close();
            tcpClient.Close();
        }
    }

    int ProState = -1;

    void FixedUpdate()
    {
        var state = transform.position - agent.transform.position;
        UnityEngine.Debug.Log(string.Format("{0:F2},{1:F2},{2:F2}", state.x, state.y, state.z));

        //REQ STATE
        string response = reader.ReadLine();
        UnityEngine.Debug.Log("서버로부터 받은 응답: " + response);

        if (response == "NewStatus")
        {
            ProState = 0;
            SendMessageToServer(string.Format("{0:F2},{1:F2}", state.x, state.z));
        }
        else if (response == "Status")
        {
            //SEND STATE

        }
        else
        {
            switch (response)
            {
                case "0":
                    agent.transform.position += Vector3.forward * 0.125f;
                    break;
                case "1":
                    agent.transform.position += Vector3.back * 0.125f;
                    break;
                case "2":
                    agent.transform.position += Vector3.left * 0.125f;
                    break;
                case "3":
                    agent.transform.position += Vector3.right * 0.125f;
                    break;
            }

            // next state
            state = transform.position - agent.transform.position;
            float reward = (Vector3.Magnitude(initPoint) - Vector3.Magnitude(state)) / Vector3.Magnitude(initPoint);
            if (done == 1)
            {
                reward = 100;
            }


            reward += -0.01f * step;

            if (step > 100)
            {
                done = 1;
                reward = -100;
            }
            SendMessageToServer(string.Format("{0:F2},{1:F2},{2:F2},{3}", state.x, state.z, reward, done));
        }

        if (done == 1)
        {
            done = 0;
            step = 0;

            Rigidbody target = agent.gameObject.GetComponent<Rigidbody>();
            Vector3 zero = Vector3.zero;
            target.velocity = zero;
            target.angularVelocity = zero;
            agent.gameObject.transform.position = new Vector3(0, 0.5f, -3.5f);

            // 🔥 CSV에서 새로운 랜덤 위치를 가져옵니다.
            Vector3 nextPosition = GetNextPositionFromCSV();

            Rigidbody target2 = gameObject.GetComponent<Rigidbody>();
            target2.velocity = zero;
            target2.angularVelocity = zero;
            transform.position = nextPosition; // 🔥 다음 위치로 이동

            initPoint = transform.position - agent.transform.position;
        }

        step++;
    }

    /// <summary>
    /// </summary>
    /// <returns>Vector3 위치</returns>
    private Vector3 GetNextPositionFromCSV()
    {
        Vector3 nextPosition = positions[currentIndex];

        currentIndex++;
        if (currentIndex >= positions.Count)
        {
            currentIndex = 0; // 500개를 전부 돌았으면 다시 처음으로
        }

        UnityEngine.Debug.Log($"다음 위치로 이동: {nextPosition}");
        return nextPosition;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
