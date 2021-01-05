using System;
using System.Threading;

namespace ServerCore
{
    class Program
    {
        static void MainThread()
        {
            while (true)
            {
                Console.WriteLine("Hello Thread!");
            }
        }
        public static void Main(string[] args)
        {
            Thread thread = new Thread(MainThread);
            thread.IsBackground = false;
            thread.Start();
            thread.Join();
            
            Console.WriteLine("Hello World!");   

        }
    }
}