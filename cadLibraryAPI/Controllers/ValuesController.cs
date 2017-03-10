using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;

namespace cadLibraryAPI.Controllers
{
    public class ValuesController : ApiController
    {
        public class Order
        {
            public string Value;
        }
        // GET api/values
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
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
