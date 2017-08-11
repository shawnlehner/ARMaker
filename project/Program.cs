using Mono.Unix;
using Mono.Unix.Native;
using Nancy.Hosting.Self;
using System;

namespace MarkerGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            var uri = "http://localhost:59005";
            Console.WriteLine("Starting Nancy on " + uri);

            HostConfiguration config = new HostConfiguration
            {
                UrlReservations = new UrlReservations()
                {
                    CreateAutomatically = true
                }
            };

            // initialize an instance of NancyHost
            var host = new NancyHost(config, new Uri(uri));
            host.Start();  // start hosting

            // check if we're running on mono
            if (Type.GetType("Mono.Runtime") != null)
            {
                // on mono, processes will usually run as daemons - this allows you to listen
                // for termination signals (ctrl+c, shutdown, etc) and finalize correctly
                UnixSignal.WaitAny(new[] {
                    new UnixSignal(Signum.SIGINT),
                    new UnixSignal(Signum.SIGTERM),
                    new UnixSignal(Signum.SIGQUIT),
                    new UnixSignal(Signum.SIGHUP)
                });
            }
            else
            {
                Console.ReadLine();
            }

            Console.WriteLine("Stopping Nancy");
            host.Stop();  // stop hosting
        }
    }
}
