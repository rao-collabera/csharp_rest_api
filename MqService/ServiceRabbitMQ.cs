using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MqService
{
    public class ServiceRabbitMQ : IHostedService
    {
            private IModel channel = null;
            private IConnection connection = null;
            private readonly IConfiguration _config;

            public ServiceRabbitMQ(IConfiguration Config)
            {
                _config = Config;
            }
            private void Run()
            {
	            var factory = new ConnectionFactory() { Uri = new Uri(_config["RabbitMQ:Uri"]) };
                connection = factory.CreateConnection();
                channel = connection.CreateModel();			
	            channel.QueueDeclare(queue: _config["RabbitMq:Queue"], exclusive: false);
                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += OnMessageRecieved;          
                channel.BasicConsume(queue: _config["RabbitMq:Queue"], autoAck: true, consumer: consumer);
            }

            public Task StartAsync(CancellationToken cancellationToken)
            {
                Run();
                return Task.CompletedTask;
            }

            public Task StopAsync(CancellationToken cancellationToken)
            {
                channel.Dispose();
                connection.Dispose();
                return Task.CompletedTask;
            }

	        private void OnMessageRecieved(object model, BasicDeliverEventArgs args)
            {
                var body = args.Body.ToArray();
                string UserName = Encoding.UTF8.GetString((byte[])args.BasicProperties.Headers.FirstOrDefault(x => x.Key == "UserId").Value);
                string message = Encoding.UTF8.GetString(body);
                Console.WriteLine("User: {0}, Message: {1}", UserName, message);
                // DoSomething
            }
    }
}