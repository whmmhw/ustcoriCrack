using System;
using System.Threading;

namespace ustcori_crack
{
    internal class Program
    {
        public static bool shouldAlert = true;
        static void Main(string[] args)
        {
            Console.Title = "Ustcori Crack V1.2.0";
            Console.WriteLine("输入链接：");
            string labLink = Console.ReadLine();
            FakeApp fakeApp = new FakeApp(labLink);
            if (fakeApp.isNewVersion)
            {
                FakeHall2 fakeHall = fakeApp.fakeHall2;
                fakeHall.UserControlModel();
            }
            else
            {
                FakeHall fakeHall = fakeApp.fakeHall;
                fakeHall.UserControlModel();
            }

            if (shouldAlert)
            {
                Console.WriteLine("按任意键退出……");
            }
            while (!shouldAlert)
            {
                Thread.Sleep(500);
            }
            Console.WriteLine("按任意键退出……");
            Console.ReadKey();
        }


    }
}
