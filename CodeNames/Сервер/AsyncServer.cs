using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class AsyncServer
    {
        private const int port = 8081;
        private static ManualResetEvent allDone = new ManualResetEvent(false);
        private static ManualResetEvent sendDone = new ManualResetEvent(false);
        private static ManualResetEvent connectDone = new ManualResetEvent(false);
        private static ManualResetEvent receiveDone = new ManualResetEvent(false);
        private static IPEndPoint endPoint;
        private static string response;
        //private static Socket socket_user1;
        private static Socket listener_user2;
        private static Socket socket_user2;
        //  private static Socket listener_user1;
        private static Socket handler;
        private static Socket socket2;
        private static StateObject state = new StateObject();
        private static int num_of_cur_user = 2;
        private static void Send(Socket client, String data)
        {
            // Convert the string data to byte data using ASCII encoding.  
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.  
            client.BeginSend(byteData, 0, byteData.Length, SocketFlags.None,
                new AsyncCallback(SendCallback), client);

            sendDone.WaitOne();
        }

        private static void SendCallback(IAsyncResult ar)
        {
            // Извлекаем сокет из объекта состояния
            Socket client = (Socket)ar.AsyncState;

            // Завершаем отправление даных 
            client.EndSend(ar);   // lock
            sendDone.Set();
            allDone.Set();
            //socket_user2.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, ReadCallBack, state);
            Receive(client);
            allDone.WaitOne();
        }



        private static void Receive(Socket client)
        {
            // Create the state object.  
            StateObject state = new StateObject();

            // передача ссылки на сокет нашему state object
            state.socket = client;

            // Начать получать данные с сервера
            client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReceiveCallback), state);

            // блокировка до окончании асинхронного приема данных
            receiveDone.WaitOne();
        }

        private static void ReceiveCallback(IAsyncResult ar)
        {
            // Извлекаем объект состояния и клинетский сокет из объекта асинхронного состояния
            StateObject state = (StateObject)ar.AsyncState;
            Socket client = state.socket;

            // Чтение данных с сервера
            int bytesRead = client.EndReceive(ar);
            // There might be more data, so store the data received so far.  
            state.textBuilder.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));
            if (bytesRead >= StateObject.BufferSize)
            {
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReceiveCallback), state);
                receiveDone.WaitOne();
            }
            else
            {
                // Все данные пришли. Записываем ответ в response
                if (state.textBuilder.Length > 1)
                {
                    response = state.textBuilder.ToString();
                }
                Console.WriteLine("You received");
                Console.WriteLine(response);
                // Переводим в сигнальное состояние
                receiveDone.Set();
                sendDone.Reset();
                if (num_of_cur_user == 2)
                {
                    num_of_cur_user = 1;
                    Send(handler, response);
                }
                else if (num_of_cur_user == 1)
                {
                    num_of_cur_user = 2;
                    Send(socket_user2, response);
                }
                sendDone.WaitOne();
            }
        }
        public void StartListenning()
        {
            string ip = "127.0.0.2";

            IPAddress iPAddress = IPAddress.Parse(ip);
            endPoint = new IPEndPoint(iPAddress, port);
            listener_user2 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            //        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //       socket.Connect(endPoint);
        /*    string ip2 = "127.0.0.1";
            IPAddress iPAddress2 = IPAddress.Parse(ip2);
            IPEndPoint endPoint2 = new IPEndPoint(iPAddress2, port);
            socket_user2 = new Socket(iPAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);
            //Connect(endPoint, socket);
            Connect(endPoint2, socket_user2);*/
            listener_user2.Bind(endPoint);
            allDone.Reset();
            listener_user2.Listen(10);

            while (true)
            {
                allDone.Reset();
                listener_user2.BeginAccept(AcceptCallBack, listener_user2);
                allDone.WaitOne();
            }
        }
        static void ReadCallBack(IAsyncResult ar)
        {
            StateObject state = (StateObject)ar.AsyncState;
            handler = state.socket;
            int bytesRead = handler.EndReceive(ar);
            string text = Encoding.ASCII.GetString(state.buffer, 0, bytesRead);
            Console.WriteLine("You received");
            Console.WriteLine(text);
            sendDone.Reset();
            Send(socket_user2, text);
            sendDone.WaitOne();
        }
        static void AcceptCallBack(IAsyncResult ar)
        {
            Console.WriteLine("Start");

            Socket listener = (Socket)ar.AsyncState;
            socket2 = listener.EndAccept(ar);

            state.socket = socket2;
            allDone.Set();
            socket2.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, ReadCallBack, state);

        }
        public static void Connect(EndPoint remoteEP, Socket client)
        {
            client.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), client);

            connectDone.WaitOne();
        }

        private static void ConnectCallback(IAsyncResult ar)
        {
            Console.WriteLine("Connected");
            // Извлекаем сокет из объекта состояния
            socket_user2 = (Socket)ar.AsyncState;

            // Ждем окончания конекта  
            socket_user2.EndConnect(ar);

            // Переключаем устройство в сигнальное состояние
            connectDone.Set();
        }
    }
}
