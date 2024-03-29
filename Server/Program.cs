﻿using System;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting RabbitMQ queue processor");
            Console.WriteLine();
            Console.WriteLine();

            var queueProcessor = new RabbitConsumer() { Enabled = true };
            queueProcessor.Start();

            Console.ReadLine();
        }
    }
}
