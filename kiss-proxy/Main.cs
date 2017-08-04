using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace kiss_proxy {

    class Program {

        private static readonly ProxyTestController controller = new ProxyTestController();

        static void Main(string[] args) {

            //Start proxy controller
            controller.StartProxy();
            
            var forwarder = new TcpForwarder(new ProxyDefinition {
                ServerAddress = IPAddress.Any,
                ServerPort = 1025,
                ClientAddress = IPAddress.Parse("172.27.50.55"),
                ClientPort = 80
            });
            forwarder.Start();

            Console.Title = "kiss proxy";
            Console.CancelKeyPress += ConsoleOnCancelKeyPress;
            Console.WriteLine("Hit CTRL+C to end session.");
            
            bool bDone = false;
            do {
                Console.WriteLine("\nEnter a command [L=List; Q=Quit]:");
                Console.Write(">");
                ConsoleKeyInfo cki = Console.ReadKey();
                Console.WriteLine();
                switch (Char.ToLower(cki.KeyChar)) {
                    case 'c':
                        Console.SetCursorPosition(1, 1);
                        Console.Write("ju");
                        break;
                    case 'd':
                        break;

                    case 'l':
                        WriteCommandResponse("listing");
                        break;

                    case 'q':
                        bDone = true;
                        break;
                }
            } while (!bDone);

            controller.Stop();
        }

        private static void ConsoleOnCancelKeyPress(object sender, ConsoleCancelEventArgs consoleCancelEventArgs) {
            
        }

        public static void WriteCommandResponse(string s) {
            ConsoleColor oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(s);
            Console.ForegroundColor = oldColor;
        }
    }
}
