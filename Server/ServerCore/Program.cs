using System;
using System.Threading;
using System.Threading.Tasks;

namespace ServerCore
{
    class Program
    {
        
        
        
        #region 경합 조건. Race Condition

        // /* 아래와 같은 상황에서 왜 0이 아닌 이상한 값이 출력될까? */
        // private static int _number = 0;
        //
        // static void Thread_1()
        // {
        //     for (int i = 0; i < 100000; i++)
        //     {
        //         // 수정 전
        //         _number++;
        //         
        //         // 수정 후: 원자적으로 덧셈을 한다.
        //         Interlocked.Increment(ref _number); // TIP. 참조를 넘겨줘야 하니까 ref!
        //     }
        // }
        //
        // static void Thread_2()
        // {
        //     for (int i = 0; i < 100000; i++)
        //     {
        //         // 수정 전
        //         _number--;
        //         
        //         // 수정 후: 원자적으로 뺄셈을 한다.
        //         Interlocked.Decrement(ref _number);
        //     }
        // }
        //
        // static void Main(string[] args)
        // {
        //     Task t1 = new Task(Thread_1);
        //     Task t2 = new Task(Thread_2);
        //     t1.Start();
        //     t2.Start();
        //
        //     Task.WaitAll(t1, t2);
        //     
        //     Console.WriteLine(_number);
        // }
        //
        // /*
        //     ==> 사실 number++는... 아래와 같다.
        //     
        //          int temp = number;
        //          temp += 1;
        //          number = temp; 
        //          
        //                 위와 같이 단계별로 진행되기 때문에 문제가 발생하는 것.
        //                 이를 atomic 이라고 한다. (원자성)
        //                 일련의 동작이 "한 번에" 일어나야 한다는 개념.
        //  */
        #endregion
        

        #region 메모리 배리어. Memory Barrier

//                 /*
//             < 하드웨어의 최적화 >
//             
//             CPU 에게 우리가 명령을 내리면, 걔가 봐서 서로 의존성이 없는 명령어라면 순서를 뒤바꿀 수 있다.
//             아래 예시에서 Store ~ 와 Load ~ 를 바꿔서 실행할 수 있다는 뜻.
//             싱글 쓰레드에서는 최적화를 하는 게 매우 좋았지만, 멀티 쓰레드는 이러면 우리가 만든 로직에 문제가 생길 수 있다.
//             
//             
//             < 메모리 배리어 >
//             
//             따라서 순서를 딱 정해주기 위해 쓰는 게 메모리 배리어다.
//             
//             역할
//             A) 코드 재배치 억제
//             B) 가시성: 1번 직원이 주문을 받았다는 것을 2번 직원이 바로 볼 수 있는가
//                       volatile 키워드, 그리고 후에 배울 lock, atomic 개념들도 이 역할을 간접적으로 수행한다.
//             
//             종류
//             1) Full Memory Barrier (ASM MFENCE, C# Thread.MemoryBarrier): Store/Load 둘 다 막는다.
//             2) Store Memory Barrier (ASM SFENCE): Store 만 막는다.
//             3) Load Memory Barrier (ASM LFENCE): Store 만 막는다.
//
//          */
//         private static int x = 0;
//         private static int y = 0;
//         private static int result1 = 0;
//         private static int result2 = 0;
//
//         static void Thread_1()
//         {
//             y = 1;            // Store y
//             
//             //---------------------------------------
//             Thread.MemoryBarrier(); // 실제 메모리의 위의 store 를 올린다. 이어서 뒤에 load 할 때는 동기화된 따끈따끈한 값을 가져온다.
//             
//             result1 = x;      // Load x
//         }
//
//         static void Thread_2()
//         {
//             x = 1;             // Store x
//             
//             //---------------------------------------
//             Thread.MemoryBarrier();
//             
//             result2 = y;       // Load y
//         }
//
//         static void Main(string[] args)
//         {
//             int count = 0;
//             
//             while (true)
//             {
//                 count++;
//                 
//                 x = y = result1 = result2 = 0;
//                 
//                 Task task1 = new Task(Thread_1);
//                 Task task2 = new Task(Thread_2);
//                 task1.Start();
//                 task2.Start();
//
//                 Task.WaitAll(task1, task2);
//                 
//                 if (result1 == 0 && result2 == 0)
//                     break;
//             }
//             
//             Console.WriteLine($"{count} 번 안에 빠져나옴.");
//         }

        #endregion


        #region 캐시 히트. Spatial Locality

        // static void Main(string[] args)
        // {
        //     
        //     int [,] arr = new int[10000, 10000];
        //
        //     /*
        //      캐시 철학 중 "Spatial Locality"
        //      배열에 공간적으로 가까이 있는 애들은 캐시에 어느 정도 저장이 되는데,
        //      따라서 가까이 있는 애들은 접근을 빨리 할 수 있다. (캐시 히트 상태)
        //      
        //      [][][][][]
        //      [][][][][]
        //      [][][][][]
        //      [][][][][]
        //      [][][][][]
        //      
        //      이차배열을 나타낸 그림 --> 여기서 우측으로 진행되면 접근이 빠르고, 세로로 진행되면 더 느리다. 
        //      
        //     */
        //     {
        //         long now = DateTime.Now.Ticks;
        //         for (int y = 0; y < 10000; y++)
        //         {
        //             for (int x = 0; x < 10000; x++)
        //             {
        //                 arr[y, x] = 1;
        //             }
        //         }
        //         long end = DateTime.Now.Ticks;
        //         Console.WriteLine($"(y, x) 순서로 걸린 시간 {end - now}");
        //     }
        //     {
        //         long now = DateTime.Now.Ticks;
        //         for (int y = 0; y < 10000; y++)
        //         {
        //             for (int x = 0; x < 10000; x++)
        //             {
        //                 arr[x, y] = 1;
        //             }
        //         }
        //         long end = DateTime.Now.Ticks;
        //         Console.WriteLine($"(x, y) 순서로 걸린 시간 {end - now}");
        //     }
        // }
        //

        #endregion


        #region 컴파일러 최적화의 문제

/*
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
*/

        #endregion


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