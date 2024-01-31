using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace CollaberaAPI
{
    /// <summary>
    /// Publish Message to RabbitMQ Queue.
    /// </summary>
    public class RabbitMqRepository : IRabbitMqRepository
    {
        private readonly IConfiguration _config;
        private readonly string _userID;

        /// <summary>
        /// The constructor
        /// </summary>
        /// <param name="Config">The configuration.</param>
        /// <param name="Context">The context.</param>
        public RabbitMqRepository(IConfiguration Config, IHttpContextAccessor Context)
        {
            _config = Config;
            _userID = Context.HttpContext.User.FindFirst("preferred_username").Value.Split('@')[0];
        }

        /// <summary>
        /// Publish Message
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message"></param>
        /// <returns></returns>
        public string SendMessage<T>(T message)
        {
            var factory = new ConnectionFactory { Uri = new Uri(_config["RabbitMq:Uri"]) };
            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();
            IBasicProperties props = channel.CreateBasicProperties();
            props.ContentType = "text/plain";
            props.DeliveryMode = 2;
            props.Headers = new Dictionary<string, object> { { "UserId", _userID } };
            channel.QueueDeclare(_config["RabbitMq:Queue"], exclusive: false);
            var body = Encoding.UTF8.GetBytes(message.ToString());
            channel.BasicPublish(exchange: "", routingKey: _config["RabbitMq:Queue"], basicProperties: props, body: body);
            return JsonConvert.SerializeObject(new { statusCode = 200, errorMessage = "" });
        }
    }
}