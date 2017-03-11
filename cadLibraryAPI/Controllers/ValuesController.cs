using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Helpers;
using System.Web.Http;
using System.Web.Http.Results;
using System.Web.Mvc;

namespace cadLibraryAPI.Controllers
{
    public class ValuesController : ApiController
    {
        public class BookEntity : TableEntity
        {
            public BookEntity()
            {
                PartitionKey = "Books";
                RowKey = Guid.NewGuid().ToString(); 
            }
            public string title { get; set; }
            public string author { get; set; }
            public string description { get; set; }
        }

        public class Order
        {
            public string Value;
        }
        // GET api/values
        public string Get()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=sattarcloudlibrary;AccountKey=0VZqOhtYFlTgkOvnvOV4uVjYN735yT74QDEM0OSc+3jTJWvocp/A8ZcNIxyxVJ07jdUIXI0Ckt1wbsL/Koco0Q==;");
            CloudTableClient cloudtableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = cloudtableClient.GetTableReference("cadlibrarytable");
            List<BookEntity> entities = table.ExecuteQuery(new TableQuery<BookEntity>()).ToList();
            string json = JsonConvert.SerializeObject(new { operations = entities });
            return json;
        }

        // GET api/values/5
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values
        public IEnumerable<string> Post(Order order)
        {
            string message = order.Value;
            ConnectionFactory factory = new ConnectionFactory() { HostName = "localhost" };
            using (IConnection connection = factory.CreateConnection())
            {
                using (IModel channel = connection.CreateModel())
                {
                    channel.QueueDeclare(queue: "cadLibrary",
                                         durable: false,
                                         exclusive: false,
                                         autoDelete: false,
                                         arguments: null);
                    byte[] body = Encoding.UTF8.GetBytes(message);

                    channel.BasicPublish(exchange: "",
                                         routingKey: "cadLibrary",
                                         basicProperties: null,
                                         body: body);
                }
            }
            return new string[] { message };
        }

        // PUT api/values/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        public void Delete(int id)
        {
        }
    }
}
