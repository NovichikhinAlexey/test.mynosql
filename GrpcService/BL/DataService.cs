using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;

namespace GrpcService.BL
{
    public class DataService: IDisposable
    {
        public static DataService Instance;

        private Timer _timer;

        private TaskCompletionSource<int> _work = new TaskCompletionSource<int>();

        private int _index = 0;

        private List<(int, DateTime)> _data = new List<(int, DateTime)>();

        private  List<(TaskCompletionSource<int>, IServerStreamWriter<Data>)> _streamList = new List<(TaskCompletionSource<int>, IServerStreamWriter<Data>)>();

        public DataService()
        {
            _timer = new Timer(DoTime, null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
            Instance = this;
        }

        private void DoTime(object state)
        {
            (int, DateTime) value;
            value.Item1 = ++_index;
            value.Item2 = DateTime.UtcNow;
            _data.Add(value);

            Console.WriteLine($"new: {value.Item1} || {value.Item2:HH:mm:ss}");

            var item = new Data() {Index = value.Item1, Time = value.Item2.ToString("HH:mm:ss")};

            foreach (var pair in _streamList.ToArray())
            {
                try
                {
                    pair.Item2.WriteAsync(item);
                }
                catch (InvalidOperationException ex) when (ex.Message == "Cannot write message after request is complete.")
                {
                    pair.Item1.TrySetResult(1);
                    _streamList.Remove(pair);
                    Console.WriteLine("Remove stream connect");
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    pair.Item1.TrySetResult(1);
                    _streamList.Remove(pair);
                    Console.WriteLine("Remove stream connect");
                }
            }
        }

        public (int, DateTime) Last => _data.LastOrDefault();

        public Task RegisterStream(IServerStreamWriter<Data> stream)
        {
            foreach (var item in _data)
            {
                stream.WriteAsync(new Data() {Index = item.Item1, Time = item.Item2.ToString("HH:mm:ss")}).Wait();
            }

            (TaskCompletionSource<int>, IServerStreamWriter<Data>) record;
            record.Item1 = new TaskCompletionSource<int>();
            record.Item2 = stream;

            

            _streamList.Add(record);

            return record.Item1.Task;
        }

        public void Dispose()
        {
            _timer?.Dispose();
            foreach (var pair in _streamList)
            {
                pair.Item1.TrySetResult(1);
                Console.WriteLine("Remove stream connect");
            }
        }

        public void Stop()
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
            foreach (var pair in _streamList)
            {
                pair.Item1.TrySetResult(1);
                Console.WriteLine("Remove stream connect");
            }
        }
    }
}
