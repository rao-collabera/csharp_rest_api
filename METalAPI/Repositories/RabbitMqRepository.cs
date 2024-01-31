using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace METalAPI
{
    /// <summary>
    /// Publish Message to RabbitMQ Queue.
    /// </summary>
    public class RabbitMqRepository(IConfiguration Config, IHttpContextAccessor Context) : IRabbitMqRepository
    {
        /// <summary>
        /// Publish Message
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message"></param>
        /// <returns></returns>
        public string SendMessage<T>(T message)
        {
            var factory = new ConnectionFactory { Uri = new Uri(Config["RabbitMq:Uri"]) };
            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();
            IBasicProperties props = channel.CreateBasicProperties();
            props.ContentType = "text/plain";
            props.DeliveryMode = 2;
            props.Headers = new Dictionary<string, object> { { "UserId", Context.HttpContext.User.FindFirst("preferred_username").Value.Split('@')[0] } };
            channel.QueueDeclare(Config["RabbitMq:Queue"], exclusive: false);
            var body = Encoding.UTF8.GetBytes(message.ToString());
            channel.BasicPublish(exchange: "", routingKey: Config["RabbitMq:Queue"], basicProperties: props, body: body);
            return JsonConvert.SerializeObject(new { statusCode = 200, errorMessage = "" });
        }
    }
}