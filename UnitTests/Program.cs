using System;
using MbUnit.Core;

namespace UnitTests
{
    class Program
    {
        static void Main(string[] args)
        {
            using (AutoRunner runner = new AutoRunner())
            {
                runner.Load();
                runner.Run();
                runner.ReportToHtml();
                Console.Write(runner.Result.ToString());
            } 
        }
    }
}
