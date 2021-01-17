using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace DummyClient
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
            
            // 휴대폰 설정
            Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            
            // 문지기에게 입장 문의
            socket.Connect(endPoint);
            Console.WriteLine($"Connected To {socket.RemoteEndPoint}");
            
            // 보낸다
            byte[] sendBuff = Encoding.UTF8.GetBytes("Hello World!");
            int sendBytes = socket.Send(sendBuff);
            
            // 받는다
            byte[] recvBuff = new byte[1024];
            int recvBytes = socket.Receive(recvBuff);
            string recvData = Encoding.UTF8.GetString(recvBuff, 0, recvBytes);
            Console.WriteLine($"[From Server] {recvData}");

            // 나간다
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();

        }
    }
}
