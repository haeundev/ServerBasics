using System;
using System.Threading;
using System.Threading.Tasks;

namespace ServerCore
{
    class Lock_AutoResetEvent
    {
        private AutoResetEvent _available = new AutoResetEvent(true);
        // --> 이 available 변수는 커널 안에서는 boolean 값 하나다.
        
        /// <summary>
        /// Spin Lock은 유저 모드에서 실행하면 되는데,
        /// AutoResetEvent나 ManualResetEvent나, 커널 모드를 사용하게 되면 그 하나를 사용하는 자체로도 부담이 된다.
        /// </summary>

        public void Acquire()
        {
            _available.WaitOne(); // 입장 시도. 처음에 true 했으니까 열려있을 것. 입장 후 문을 자동으로 닫음.
            // 참고로, 문을 닫는 것도 별도 함수로 있음.  _available.Reset(); 커널의 boolean을 false로.
        }

        public void Release()
        {
            _available.Set(); // 다시 커널의 boolean을 true로.
        }
    }   
    
    class Lock_ManualResetEvent
    {
        private ManualResetEvent _available = new ManualResetEvent(true);

        public void Acquire()
        {
            _available.WaitOne(); // 입장 시도. 처음에 true 했으니까 열려있을 것. 그러나 문을 자동으로 닫지는 않음.
            _available.Reset();
            // 이 두 개를 묶어서 원자적으로 실행하지 않으면 틀린 결과가 나올 것.
        }

        public void Release()
        {
            _available.Set();
        }
    }

    class MySpinLock  /* "화장실에 사람이 나올 때까지 기다리자! */
    {
        private volatile bool _locked1 = false;
        private volatile int _locked2 = 0;
        
        public void Acquire()
        {
            // 시도 1.
                while (_locked1)
                {
                    // 잠김이 풀릴 때까지 기다리겠다.
                }
            
                // 내꺼!
                _locked1 = true;

                // ==> 시도 1 실패. 왜? (1) 들어와서 (2) 문을 잠그는 것이 두 개에 나눠서 실행되니까!

            // 시도 2.
                while (_locked1)
                {
                    int original = Interlocked.Exchange(ref _locked2, 1);
                    // _locked2에 1을 넣는데, 만약 original 도 1이라면 다른 누군가가 이미 점유하고 있다는 뜻.
                    if (original == 0)
                        break;
                }

            // 시도 3.
                // CAS: Compare-And-Swap 형식의 메서드
                while (_locked1)
                {
                    int original2 = Interlocked.CompareExchange(ref _locked2, 1, 0);
                    if (original2 == 0)
                        break;
                    
                    // 이걸 좀 더 똑똑하게 짜면,
                    int expected = 0;
                    int desired = 1;
                    if (Interlocked.CompareExchange(ref _locked2, desired, expected) == expected)
                        break;
                    
                    
                    /*  Context Switching   "나 일단 자리로 갈게!"  */
                    // "쉬다 올게..." ==> 3개 중 아무 거나 쓰면 됨.
                    Thread.Sleep(1); // 무조건 휴식 --> 1ms 정도 쉬고 싶어요. ==> 실제 몇 밀리seconds를 쉴 지는 스케줄러가 정함.
                    Thread.Sleep(0); // 조건부 양보 --> 나보다 우선순위가 낮은 애들한테는 양보 불가. 그런 경우엔 다시 나에게.
                    Thread.Yield(); // 관대한 양보 --> 지금 실행 가능한 쓰레드가 있으면 실행해라. 실행 가능한 애가 없다면 나에게.
                    // ==> 근데 이렇게 하는 게 레지스터... 뭐랑 엮여서 운영체제가 다시 뭘 할당해주고 그런 과정이 커널 모드에 있어서,
                    //     차라리 유저 모드에서 spin lock 해놓는 상황이 더 좋을 수도 있다.
                }
        }

        public void Release() // 이미 문을 잠그고 들어왔기 때문에, 문을 다시 열어주는 건 설렁설렁 해도 됨~
        {
            _locked1 = false;
            
            //
            _locked2 = 0;
        }
    }
    
    class Program
    {
        #region Thread Local Storage  "각각 지니고 다니는 쟁반!"

        private static ThreadLocal<string> _threadName = new ThreadLocal<string>();
        // ThreadLocal로 매핑해서, 한 쓰레드가 threadName을 건드려도 다른 쓰레드들의 threadName에 영향을 주지 않는다.

        private static ThreadLocal<string> _threadName2 = 
            new ThreadLocal<string>(() => $"My name is {Thread.CurrentThread.ManagedThreadId}");

        static void WhoAmI()
        {
            _threadName.Value = $"My name is {Thread.CurrentThread.ManagedThreadId}";
            Console.WriteLine(_threadName.Value);

            // 또는

            bool repeat = _threadName2.IsValueCreated;
            if (repeat)
                Console.WriteLine(_threadName2.Value + " (repeat)");
            else
                Console.WriteLine(_threadName2.Value);
        }

        static void Main(string[] args)
        {
            Parallel.Invoke(WhoAmI, WhoAmI, WhoAmI, WhoAmI, WhoAmI);
            
            _threadName.Dispose();
        }

        #endregion
        

        #region ReaderWriterLock

        // private static object _lock = new object();
        // private static SpinLock _lock2 = new SpinLock();
        //
        // static void Main(string[] args)
        // {
        //     // 버전 1
        //     lock (_lock)
        //     {
        //         
        //     }
        //
        //     // 버전 2
        //     bool lockTaken = false;
        //     try
        //     {
        //         _lock2.Enter(ref lockTaken);
        //     }
        //     finally
        //     {
        //         if (lockTaken)
        //             _lock2.Exit();
        //     }
        // }
        //
        // // 원래는 위의 버전 두 개 중에 쓰면 된다. 그런데 가끔은 너무 적은 빈도로 일어나는 쓰기 때문에 락을 사용해야 하는 경우가 있다.
        // // 이런 경우에 사용할 수 있는 것이 RWLock.
        //
        // class Reward
        // {
        //     
        // }
        //
        // private static ReaderWriterLockSlim _lock3 = new ReaderWriterLockSlim();
        //
        // static Reward GetRewardById(int id) // 평소에 읽기만 할 때는 동시다발적으로 접근하다가,
        // {
        //     _lock3.EnterReadLock();
        //     _lock3.ExitReadLock();
        //     
        //     return null;
        // }
        //
        // static void AddReward(Reward reward) // 아주 가끔 보상을 추가할 때, write할 때는 제대로 잠금.
        // {
        //     _lock3.EnterWriteLock();
        //     _lock3.ExitWriteLock();
        // }

        #endregion


        #region SpinLock

        // private static int _num = 0;
        // private static SpinLock _spinLock = new SpinLock();
        //
        // static void Thread_1()
        // {
        //     for (int i = 0; i < 100000; i++)
        //     {
        //         _spinLock.Acquire();
        //         _num++;
        //         _spinLock.Release();
        //     }
        // }
        //
        // static void Thread_2()
        // {
        //     for (int i = 0; i < 100000; i++)
        //     {
        //         _spinLock.Acquire();
        //         _num--;
        //         _spinLock.Release();
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
        //     Console.WriteLine(_num);
        // }

        #endregion


        #region 상호 배제(Mutual Exclusive)와 락(lock)

        //
        // private static int _number = 0;
        // private static object _obj = new object();
        //
        // static void Thread_1()
        // {
        //     for (int i = 0; i < 100000; i++)
        //     {
        //         /* 상호 배제. Mutual Exclusive  (C++의 std::mutex) */
        //         Monitor.Enter(_obj); // 문을 잠그는 역할. 다른 애들이 여기에 접근할 수 없게.
        //         _number++;
        //         Monitor.Exit(_obj);
        //         // ==> 이 블럭이 싱글 쓰레드라고 가정하고 사용할 수 있어서 편하기도 하지만,
        //         //     문을 닫았다 열었다 하는 게 너무 복잡해질 수도 있고, 더 심각하게는
        //         //     문을 다시 열어주지 않고 뭔가 리턴하거나 한다면... 다른 애들이 무한대로 대기하는 문제 발생.
        //         //        ==> 이 상황을 데드 락 "Dead Lock"이라고 한다.
        //     }
        //     
        //     // 한 가지 해결 방법은 try finally 를 사용하는 것.
        //     for (int i = 0; i < 100000; i++)
        //     {
        //         try
        //         {
        //             Monitor.Enter(_obj);
        //             _number++;
        //             return;
        //         }
        //         finally
        //         {
        //             Monitor.Exit(_obj); // 이 부분은 try 의 성공 여부와 상관없이 무조건 한 번은 실행되니까.
        //         }
        //     }
        //     
        //     // 그러나 이것도 번거로움~  대부분의 경우 lock을 사용한다.
        //     // lock 도 내부적으로는 Monitor.Enter/Exit 으로 구현되어 있지만, 사용이 좀 더 편리함.
        //     for (int i = 0; i < 100000; i++)
        //     {
        //         lock (_obj)
        //         {
        //             _number++;
        //         }
        //     }
        // }
        //
        // static void Thread_2()
        // {
        //     for (int i = 0; i < 100000; i++)
        //     {
        //         Monitor.Enter(_obj);
        //         _number--;
        //         Monitor.Exit(_obj);
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

        #endregion


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