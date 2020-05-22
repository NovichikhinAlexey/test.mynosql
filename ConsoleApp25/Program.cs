using System;
using System.Threading.Tasks;
using MyNoSqlServer.Abstractions;
using MyNoSqlServer.DataReader;

namespace ConsoleApp25
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var writer = new MyNoSqlServer.DataWriter.MyNoSqlServerDataWriter<Entity>(() => "http://localhost:5123", "Entity");
            
            await writer.InsertOrReplaceAsync(new Entity(1, "1111"));
            await writer.InsertOrReplaceAsync(new Entity(2, "2222"));

            await writer.CleanAndKeepMaxPartitions(0);

            int i = 1;
            var t = Task.Run(async () =>
            {
                while (true)
                {
                    await writer.InsertOrReplaceAsync(new Entity(i, $"{i}{i}{i}"));
                    Console.WriteLine($"-->{i}");
                    await Task.Delay(5000);
                    i++;
                }
            });

            await Task.Delay(10000);

            var client = new MyNoSqlTcpClient(() => "http://localhost:5125", "TestApp");
            
            var reader = new MyNoSqlServer.DataReader.MyNoSqlReadRepository<Entity>(client, "Entity");

            client.Start();

            var r = reader.Count();
            Console.WriteLine($"count: {r}");

            reader.SubscribeToChanges(list =>
            {
                foreach (var entity in list)
                {
                    Console.WriteLine($"<-- {entity.Id}");
                }
            });

            Console.ReadLine();

            Console.WriteLine($"count: {r}");

            Console.ReadLine();

            /*
output:
-->1
-->2
-->3
count: 0
-->4
-->5

count: 0
        -->6
              */

        }
    }

    public class Entity : IMyNoSqlEntity
    {
        public Entity()
        {
        }

        public Entity(int id, string name)
        {
            Id = id;
            Name = name;
            PartitionKey = "Entity";
            RowKey = id.ToString();
            TimeStamp = DateTime.UtcNow;
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTime TimeStamp { get; set; }
        public DateTime? Expires { get; set; }
    }
}
