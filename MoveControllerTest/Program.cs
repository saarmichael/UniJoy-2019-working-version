using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MoveController;

namespace MoveControllerTest
{
    class Program
    {
        static void Main(string[] args)
        {
            RemoteController mc = new RemoteController();

            while(true)
            {
                if (mc.IsStartButtonPressed())
                {
                    Console.WriteLine("yesss");
                }
                int value = mc.SubjectMovementChoice();
                if (value == 1)
                {
                    Console.WriteLine("left");
                }
                if (value == 2)
                {
                    Console.WriteLine("right");
                }
                if (value == 10)
                {
                    Console.WriteLine("yesss");
                }
                //Console.WriteLine(mc.SubjectMovementChoice());
            }
        }
    }
}
