using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ServerCore
{
    class Program
    {
        private static Listener _listener = new Listener();

        private static void OnAcceptHandler(Socket clientSocket)
        {
            try
            {
                // 받기
                byte[] recvBuff = new byte[1024];
                int recvBytes = clientSocket.Receive(recvBuff); // 몇 바이트를 받아왔는가
                string recvData = Encoding.UTF8.GetString(recvBuff, /*시작 인덱스*/0, recvBytes);
                Console.WriteLine($"[From Client] {recvData}");

                // 보내기
                byte[] sendBuff = Encoding.UTF8.GetBytes("Welcome To Server!");
                clientSocket.Send(sendBuff);

                // 쫓아내기
                clientSocket.Shutdown(SocketShutdown.Both);
                clientSocket.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                throw;
            }
        }

        static void Main(string[] args)
        {
            // DNS (Domain Name System) : www.bbo.com 도메인의 주소를 찾아내줌.
            string host = Dns.GetHostName();
            IPHostEntry ipHost = Dns.GetHostEntry(host);
            IPAddress ipAddr = ipHost.AddressList[0];
            IPEndPoint endPoint = new IPEndPoint( /*주소*/ipAddr, /*문*/7777);
            
            // "문지기야, 우리의 endpoint는 이거고, 혹시 누가 들어오면 이 handler 로 알려줘."
            _listener.Init(endPoint, OnAcceptHandler);
            Console.WriteLine("Listening ...");

            while (true)
            {

            }
        }
    }
}
