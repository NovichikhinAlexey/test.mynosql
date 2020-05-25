using System;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using GrpcService;
using static System.String;

namespace GrpcClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            Console.WriteLine("Press enter to start...");
            Console.ReadLine();


            var channel = GrpcChannel.ForAddress("http://localhost:5001");

            var client = new Greeter.GreeterClient(channel);

            var resp = await client.SayHelloAsync(new HelloRequest() {Name = "Alex"});
            
            Console.WriteLine($"Hello response: {resp.Message}");



            while (true)
            {
                Console.WriteLine("Press enter to continue...");
                var cmd = Console.ReadLine();

                if (!IsNullOrEmpty(cmd))
                    break;

                var last = await client.GetLastAsync(new Empty());

                Console.WriteLine($"Last data: index={last.Index}; time={last.Time}");
            }

            while (true)
            {
                Console.WriteLine("Try get updates...");

                try
                {
                    using var updates = client.GetUpdates(new Empty());

                    await foreach (var item in updates.ResponseStream.ReadAllAsync())
                    {
                        Console.WriteLine($"Update data: index={item.Index}; time={item.Time}");
                    }

                    Console.WriteLine("End of stream");
                }
                catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
                {
                    Console.WriteLine("Stream cancelled.");
                }
                catch (RpcException ex) when (ex.StatusCode == StatusCode.Internal)
                {
                    Console.WriteLine($"Internal error: {ex.StatusCode};;; {ex.Message}");
                }
                catch (RpcException ex)
                {
                    Console.WriteLine($"RpcException. {ex.Status}; {ex.StatusCode}");
                    Console.WriteLine(ex.ToString());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"exception: {ex.GetType().Name}");
                    Console.WriteLine(ex.ToString());
                }

                await Task.Delay(5000);
            }

        }
    }
}
