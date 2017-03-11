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
using System.Web.Script.Serialization;
using System.IO;
using System.Web;

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

        public static void SendPushNotification()
        {

            try
            {

                string applicationID = "AAAAQv1KMgU:APA91bGaJbEwCGeiabNM5JxjJXGCzirp-FHHxva70e832sN_1CrstqXfVQ15RM_FWcb8I0Oh4jci0V9g7KBQb5o77bIuLhfq5dVaik-aY7ReE45bsRt8a_walM3XDmYeMMGsbPBAaJoj";

                string senderId = "287717339653";

                string deviceId = "fQpwt0BzWok:APA91bEBDU4SKPUDmFYXCCY_bGVPw5PMiieE_u-8CIEz5IhX0VJHDTOpe36QZr0Aftt6B3n-k5rWB8k3jP65LfoUwgs1Nlls1rgHtgcEr3cAvMoOlmf6o4mLMOZaZRRhhETiqlpRFUp1";

                WebRequest tRequest = WebRequest.Create("https://fcm.googleapis.com/fcm/send");
                tRequest.Method = "post";
                tRequest.ContentType = "application/json";
                var data = new
                {
                    to = deviceId,
                    notification = new
                    {
                        body = "Successfully added",
                        title = "Book added",
                        sound = "Enabled"

                    }
                };
                var serializer = new JavaScriptSerializer();
                var json = serializer.Serialize(data);
                Byte[] byteArray = Encoding.UTF8.GetBytes(json);
                tRequest.Headers.Add(string.Format("Authorization: key={0}", applicationID));
                tRequest.Headers.Add(string.Format("Sender: id={0}", senderId));
                tRequest.ContentLength = byteArray.Length;
                using (Stream dataStream = tRequest.GetRequestStream())
                {
                    dataStream.Write(byteArray, 0, byteArray.Length);
                    using (WebResponse tResponse = tRequest.GetResponse())
                    {
                        using (Stream dataStreamResponse = tResponse.GetResponseStream())
                        {
                            using (StreamReader tReader = new StreamReader(dataStreamResponse))
                            {
                                String sResponseFromServer = tReader.ReadToEnd();
                                string str = sResponseFromServer;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string str = ex.Message;
            }
        }

    }
}
