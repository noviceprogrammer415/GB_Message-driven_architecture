﻿using System.Text;
using Messaging.Interfaces;
using RabbitMQ.Client;

namespace Messaging.Producers
{
    public class ProducerDirect : IProducer
    {
        private readonly string _queueName;
        private readonly string _hostName;

        public ProducerDirect(string hostName, string queueName)
        {
            _hostName = hostName;
            _queueName = queueName;
        }

        public void Send(string message)
        {
            var factory = new ConnectionFactory { HostName = _hostName };
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.ExchangeDeclare(exchange: "direct_exchange",
                type: "direct",
                durable: false,
                autoDelete: false,
                arguments: null);

            var body = Encoding.UTF8.GetBytes(message);

            channel.BasicPublish(exchange: "direct_exchange",
                routingKey: _queueName,
                basicProperties: null,
                body: body);
        }
    }
}
