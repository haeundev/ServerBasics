﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace ServerCore
{
    class Program
    {
        // 쓰레드들은 각자 스택 메모리를 할당받아 사용하는데, 전역변수들은 모든 쓰레드가 공통으로 사용 가능.
        // ==> 그런데, 이게 괜찮을까?
        static volatile bool _stop = false; 
        // volatile 은 최적화를 하지 말라는 뜻. 주로 사용은 피하는 편. 앞으로도 쓸 일은 없을 듯.

        static void ThreadMain()
        {
            Console.WriteLine("쓰레드 시작!");

            while (!_stop)
            {
                // 누군가가 stop 신호를 주기를 기다림.
            }
            Console.WriteLine("쓰레드 종료!");
        }
        static void Main(string[] args)
        {
            Task t = new Task(ThreadMain);
            t.Start();
            
            Thread.Sleep(1000); // 1초 동안 대기

            _stop = true;
            
            Console.WriteLine("Stop 호출");
            Console.WriteLine("종료 대기 중");
            t.Wait(); // Thread.Join 같은 기능. Task가 끝날 때까지 기다림.
            Console.WriteLine("종료 성공");
        }




        #region 쓰레드 연습 
        /*
        static void MainThread(object state)
        {
            for (int i = 0; i < 5; i++)
            {
                Console.WriteLine("Hello Thread!");
            }
        }

        public static void Main(string[] args)
        {
            ThreadPool.SetMinThreads(1, 1);
            ThreadPool.SetMaxThreads(5, 5);
            
            for (int i = 0; i < 5; i++)
            {
                // 오래 걸리는 일이 있을 때는 쓰레드를 만들지 않고 Task 활용.
                Task task = new Task(() => { while (true) { } }, TaskCreationOptions.LongRunning);
                task.Start();
            }

            #region 단기알바 채용하기
            ThreadPool.QueueUserWorkItem(MainThread);
            #endregion
            
            #region 정직원 채용하기
            Thread thread = new Thread(MainThread); // param: 쓰레드가 시작되면 그 쓰레드의 메인 함수가 됨.
            thread.Name = "TestThread";
            thread.IsBackground = true; // param: 메인이 종료되면 백그라운드 쓰레드도 종료.
            thread.Start();
            thread.Join(); // 쓰레드가 끝날 때까지 기다렸다가 이 뒷부분 실행.
            #endregion

            Console.WriteLine("Hello World!");   

        }
        */
        #endregion
    }
}