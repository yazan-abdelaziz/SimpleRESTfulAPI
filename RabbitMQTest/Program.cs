using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nest;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RabbitMQTest
{
    public class Program
    {
        public IConfiguration Configuration { get; }
        public readonly ElasticClient _elasticClient;
        public readonly List<Employee> _cache;
        public static ESInterface _esi;
        

        public Program(IConfiguration configuration)
        {
            Configuration = configuration;

        }

        public static void Main(string[] args)
        {
            var serviceProvider = new ServiceCollection()
                .AddSingleton<ElasticClient, ElasticClient>()
                .AddSingleton<ESInterface, ESInterface>()
                .AddDbContext<EmpContext>(options => options.UseSqlServer("Server=.;Database=EmpData;Integrated Security=True;"))
                .AddSingleton<EmpContext, EmpContext>()
                .AddElasticsearch()
                .BuildServiceProvider();
            ESInterface el = serviceProvider.GetService<ESInterface>();


            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "task-queue",
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += async (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    Console.WriteLine(" [x] Received {0}", message);
                    string[] arr = message.Split(" ");
                    string Event = arr[0];
                    int id = int.Parse(arr[1]);
                    Employee emp = serviceProvider.GetService<EmpContext>().Employees.FirstOrDefault(e => e.EmployeeId == id);
                    if (Event.Equals("Create") || Event.Equals("Update"))
                    {
                        await el.SaveSingleAsync(emp);
                    } else
                    {
                        if (Event.Equals("Delete"))
                        {
                            await el.DeleteAsync(id);
                        }
                    }
                    
                };

                channel.BasicConsume(queue: "task-queue",
                                     autoAck: true,
                                     consumer: consumer);

                Console.WriteLine(" Press [enter] to exit.");
                Console.ReadLine();

            }
        }
    }
}
