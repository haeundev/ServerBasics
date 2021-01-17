using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ServerCore
{
    class Program
    {
        static void Main(string[] args)
        {
            // DNS (Domain Name System) : www.bbo.com 도메인의 주소를 찾아내줌.
            string host = Dns.GetHostName();
            IPHostEntry ipHost = Dns.GetHostEntry(host);
            IPAddress ipAddr = ipHost.AddressList[0];
            IPEndPoint endPoint = new IPEndPoint(/*주소*/ipAddr, /*문*/7777);
            
            // Listen 소켓 (문지기의 휴대폰) 만들기
            Socket listenSocket = new Socket( /*IP 버전*/endPoint.AddressFamily, /*TCP나 UDP 중 선택*/ SocketType.Stream,
                ProtocolType.Tcp);

            try
            {
                // 문지기 교육
                listenSocket.Bind(endPoint);
            
                // 영업 시작
                listenSocket.Listen(/*문지기의 안내 전 최대 대기 수*/10);

                while (true)
                {
                    Console.WriteLine("Listening ...");
                
                    // 손님 입장시키기
                    // 대리인의 휴대폰
                    Socket clientSocket = listenSocket.Accept();
                
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
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                throw;
            }
        }
    }
}
