using System;
using System.Runtime.InteropServices.ComTypes;
using InterfaceDynamicEnhancement;

namespace TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var core = new DefaultTest();
            var append = new object();
            var handler = new DefaultEnhancementHandler<ITest>();
            var b = handler.EnhancementObjectAsync<ITest2>(core, append);
            var c = b.TestMethod1();
            var d = b.TestMethod2(3);
            var e = b.TestMethod3(string.Empty);
            Console.WriteLine("Hello World!");
        }
    }

    public interface ITest
    {
        int TestMethod1();

        int TestMethod2(int a);

        string TestMethod3(string a);
    }

    public interface ITest2:ITest
    { }

    public class DefaultTest : ITest
    {
        public int TestMethod1()
        {
            return 5;
        }

        public int TestMethod2(int a)
        {
            return a + 5;
        }

        public string TestMethod3(string a)
        {
            return a + " " + DateTime.UtcNow;
        }
    }
}
