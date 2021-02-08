using System;
using System.Net;
using System.Net.Sockets;

namespace ServerCore
{
    public class Listener
    {
        private Socket _listenSocket;
        private Action<Socket> _onAcceptHandler;
        
        public void Init(IPEndPoint endPoint, Action<Socket> onAcceptHandler)
        {
            // Listen 소켓 (문지기의 휴대폰) 만들기
            _listenSocket = new Socket( /*IP 버전*/endPoint.AddressFamily, /*TCP나 UDP 중 선택*/ SocketType.Stream, ProtocolType.Tcp);
            _onAcceptHandler = onAcceptHandler;

            // 문지기 교육
            _listenSocket.Bind(endPoint);

            // 영업 시작
            _listenSocket.Listen( /*문지기의 안내 전 최대 대기 수*/10);

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);
            RegisterAccept(args); // 여기서 바로 되면 되고 아님 말고. 위에서 이벤트로는 어떻게든 날아오니까.
        }

        // 밑의 두 함수가 뱅뱅 돌면서 자기들이 알아서 기다리고, 다시 등록하고.
        private void RegisterAccept(SocketAsyncEventArgs args)
        {
            args.AcceptSocket = null;
            
            bool pending = _listenSocket.AcceptAsync(args);
            if (!pending)
                OnAcceptCompleted(null, args);
        }

        private void OnAcceptCompleted(object sender, SocketAsyncEventArgs args)
        {
            if (args.SocketError == SocketError.Success)
            {
                _onAcceptHandler.Invoke(args.AcceptSocket); // 지금 접속해서 들어온 애가 accept socket으로 넘어옴.
            }
            else
            {
                Console.WriteLine(args.SocketError.ToString());
            }
            
            RegisterAccept(args); // 이제 모든 게 끝났으니, 다음 아이를 위해 다시 register.
                                  // 성능은 좋아지지만, args 를 재사용할 때 조심해야 할 것 : 기존의 잔재를 없애야 한다. 위 함수의 args.AcceptSocket = null;
        }

        public Socket Accept()
        {
            return _listenSocket.Accept();  // blocking 계열의 함수. 게임에서는 사용하지 않는 것이 좋다. 비동기를 같이 사용해야 한다.
        }
    }
}