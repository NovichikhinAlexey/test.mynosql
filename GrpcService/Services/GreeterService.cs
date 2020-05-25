using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using GrpcService.BL;
using Microsoft.Extensions.Logging;

namespace GrpcService
{
    public class GreeterService : Greeter.GreeterBase
    {
        private readonly ILogger<GreeterService> _logger;
        private readonly DataService _dataService;

        public GreeterService(ILogger<GreeterService> logger, DataService dataService)
        {
            _logger = logger;
            _dataService = dataService;
        }

        public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
            return Task.FromResult(new HelloReply
            {
                Message = "Hello " + request.Name
            });
        }

        public override Task<Data> GetLast(Empty request, ServerCallContext context)
        {
            var last = _dataService.Last;
            return Task.FromResult(new Data() {Index = last.Item1, Time = last.Item2.ToString("HH:mm:ss")});
        }

        public override Task GetUpdates(Empty request, IServerStreamWriter<Data> responseStream, ServerCallContext context)
        {
            Console.WriteLine($"New stream connect. peer:{context.Peer}");
            return _dataService.RegisterStream(responseStream);
        }
    }
}
