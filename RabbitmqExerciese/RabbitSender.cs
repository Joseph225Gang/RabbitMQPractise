using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml.Serialization;

namespace Client
{
    public class RabbitSender : IDisposable
    {
        private const string HostName = "localhost";
        private const string UserName = "guest";
        private const string Password = "guest";
        private const string ExchangeName = "MyExchange";
        private const bool IsDurable = true;
        private const string InputFile = "BigFile.txt";
        private const int ChunkSize = 4096;

        private ConnectionFactory _connectionFactory;
        private IConnection _connection;
        private IModel _model;

        public RabbitSender()
        {
            SetupRabbitMq();
        }

        /// <summary>
        /// Sets up the connections for rabbitMQ
        /// </summary>
        private void SetupRabbitMq()
        {
            _connectionFactory = new ConnectionFactory
            {
                HostName = HostName,
                UserName = UserName,
                Password = Password
            };

            _connection = _connectionFactory.CreateConnection();
            _model = _connection.CreateModel();
        }

        public void Send(string message, string routingKey)
        {
            //Setup properties
            var properties = _model.CreateBasicProperties();
            properties.SetPersistent(false);

            var outputFileName = string.Format("{0}.txt", Guid.NewGuid().ToString());

            var fileStream = File.OpenRead(InputFile);
            var streamReader = new StreamReader(fileStream);
            int remaining = (int)fileStream.Length;
            int length = (int)fileStream.Length;
            var messageCount = 0;
            byte[] buffer  = new byte[ChunkSize]; ;

            while (true)
            {
                if (remaining <= 0)
                    break;

                //Read Chunk                        
                int read = 0;
                if (remaining > ChunkSize)
                {
                    buffer = new byte[ChunkSize];
                    read = fileStream.Read(buffer, 0, ChunkSize);
                }
                else
                {
                    buffer = new byte[remaining];
                    read = fileStream.Read(buffer, 0, remaining);
                }


                //Setup properties
                properties.Headers = new Dictionary<string, object>();
                properties.Headers.Add("OutputFileName", outputFileName);
                properties.Headers.Add("SequenceNumber", messageCount);

                //Send message
                //Console.WriteLine("Sending chunk message - Index = {0}; Length = {0}", messageCount, read);

                messageCount++;
                remaining = remaining - read;
            }

            //Send message
            _model.BasicPublish(ExchangeName, routingKey, properties, buffer);
        }

        public void Dispose()
        {
            if (_connection != null)
                _connection.Close();

            if (_model != null && _model.IsOpen)
                _model.Abort();

            _connectionFactory = null;

            GC.SuppressFinalize(this);
        }

        private static byte[] SerializeMessage(object message)
        {
           var jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(message);
           return Encoding.Default.GetBytes(jsonString);
        }

    }
}
