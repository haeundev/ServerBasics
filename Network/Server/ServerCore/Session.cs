using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ServerCore
{
    public class Session
    {
        private Socket _socket;
        private int _disconnectCount = 0;
        
        public void Start(Socket socket)
        {
            _socket = socket;
            SocketAsyncEventArgs recvArgs = new SocketAsyncEventArgs();
            recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnReceiveCompleted);
            recvArgs.UserToken = this; // 식별자로 구분하고 싶다면 사용. 안 써도 됨.
            
            // 데이터를 받을 버퍼
            recvArgs.SetBuffer(new byte[1024], 0, 1024);
            
            RegisterReceive(recvArgs);
        }

        public void Send(byte[] sendBuff)
        {
            _socket.Send(sendBuff);
        }

        public void Disconnect()
        {
            // 이미 1로 설정되었다면 하지 말아라. disconnect 2번 하면 오류나서.
            if (Interlocked.Exchange(ref _disconnectCount, 1) == 1)
                return;
            
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
        }
        
        #region 네트워크 통신
        private void RegisterReceive(SocketAsyncEventArgs args)
        {
            bool pending = _socket.ReceiveAsync(args);
            if (!pending)
                OnReceiveCompleted(null, args);
        }

        private void OnReceiveCompleted(object sender, SocketAsyncEventArgs args)
        {
            if (args.BytesTransferred > 0 /* 연결이 끊기면 0일 수 있음*/
                && args.SocketError == SocketError.Success)
            {
                try
                {
                    string recvData = Encoding.UTF8.GetString(args.Buffer, args.Offset, args.BytesTransferred);
                    Console.WriteLine($"[From Client] {recvData}");
                
                    RegisterReceive(args);
                }
                catch (Exception e)
                {
                    Console.WriteLine("OnReceiveCompleted Failed");
                }
            }
            else
            {
                
            }
        }
        #endregion
    }
}