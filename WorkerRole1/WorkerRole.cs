using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace WorkerRole1
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        public override void Run()
        {
            Trace.TraceInformation("WorkerRole1 is running");

            try
            {
                this.RunAsync(this.cancellationTokenSource.Token).Wait();
            }
            finally
            {
                this.runCompleteEvent.Set();
            }
        }

        public override bool OnStart()
        {
            ServicePointManager.DefaultConnectionLimit = 12;

            bool result = base.OnStart();

            Trace.TraceInformation("WorkerRole1 has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("WorkerRole1 is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("WorkerRole1 has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Working");
                GetMsgFromRabbit_toTableInsert();

                await Task.Delay(10000);
            }
        }

        public static void GetMsgFromRabbit_toTableInsert()
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "cadLibrary",
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

                Console.WriteLine(" [*] Waiting for messages.");

                try
                {
                    var consumer = new QueueingBasicConsumer(channel);
                    channel.BasicConsume(queue: "cadLibrary",
                                         noAck: true,
                                         consumer: consumer);
                    var ea = (BasicDeliverEventArgs)consumer.Queue.Dequeue();
                    var body = ea.Body;
                    var message = Encoding.UTF8.GetString(body);
                    toTableInsert(message);
                    Thread.Sleep(5000);
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
               
            }
        }

        public static string toTableInsert(string msg)
        {
            string[] message = msg.Split('+');
            BookEntity bk = new BookEntity();

            bk.title = message[0];
            bk.author = message[1];
            bk.description = message[2];
            BooksLibrary.CreateIfNotExist(bk);

            return "Done!";
        }
    }
}
