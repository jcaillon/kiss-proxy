using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kiss_proxy {
    class Program {

        private static readonly ProxyTestController controller = new ProxyTestController();

        static void Main(string[] args) {

            //Start proxy controller
            controller.StartProxy();

            Console.WriteLine("Hit any key to exit..");
            Console.WriteLine();
            Console.Read();

            controller.Stop();
        }
    }
}
