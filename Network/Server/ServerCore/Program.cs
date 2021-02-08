using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ServerCore
{
    class Program
    {
        private static Listener _listener = new Listener();

        private static void OnAcceptHandler(Socket clientSocket)
        {
            try
            {
                // 여기처럼 안 하고 pooling 방식을 사용할 수도 있음.
                Session session = new Session();
                session.Start(clientSocket);
                
                byte[] sendBuff = Encoding.UTF8.GetBytes("Welcome To Server!");
                session.Send(sendBuff);
                
                Thread.Sleep(1000);
                
                session.Disconnect();
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
