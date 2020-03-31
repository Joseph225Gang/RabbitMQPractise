using RabbitMQ.Client;
using RabbitMQ.Client.MessagePatterns;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Server2
{
    public class RabbitConsumer : IDisposable
    {
        private const string HostName = "localhost";
        private const string UserName = "guest";
        private const string Password = "guest";
        private const string QueueName = "CQueue";

        public delegate void OnReceiveMessage(string message);

        public bool Enabled { get; set; }

        private ConnectionFactory _connectionFactory;
        private IConnection _connection;
        private IModel _model;
        private Subscription _subscription;

        public RabbitConsumer()
        {
            _connectionFactory = new ConnectionFactory
            {
                HostName = HostName,
                UserName = UserName,
                Password = Password
            };

            _connection = _connectionFactory.CreateConnection();
            _model = _connection.CreateModel();
            _model.BasicQos(0, 1, false);
        }

        public void Start()
        {
            _subscription = new Subscription(_model, QueueName, false);

            var consumer = new ConsumeDelegate(Poll);
            consumer.Invoke();
        }
        private delegate void ConsumeDelegate();

        private void Poll()
        {
            while (Enabled)
            {
                //Get next message
                var deliveryArgs = _subscription.Next();

                //Get Output File Path
                var pathProperty = (byte[])deliveryArgs.BasicProperties.Headers["OutputFileName"];
                var outputPath = Encoding.Default.GetString(pathProperty);
                var sequenceNumber = (int)deliveryArgs.BasicProperties.Headers["SequenceNumber"];


                //Adding message                
                using (var fileStream = new FileStream(outputPath, FileMode.Append, FileAccess.Write))
                {
                    fileStream.Write(deliveryArgs.Body, 0, deliveryArgs.Body.Length);
                    fileStream.Flush();
                }

                //Acknowledge message is processed
                _subscription.Ack(deliveryArgs);
            }
        }


        public void Dispose()
        {
            if (_model != null)
                _model.Dispose();
            if (_connection != null)
                _connection.Dispose();

            _connectionFactory = null;

            GC.SuppressFinalize(this);
        }
    }
}
