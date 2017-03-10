using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;


namespace WorkerRole1
{
    public class BookEntity: TableEntity
    {
        public BookEntity()
        {
            this.PartitionKey = "Books";
            this.RowKey = Guid.NewGuid().ToString();
        }
        public string title { get; set; }
        public string author { get; set; }
        public string description { get; set; }
    }

    class BooksLibrary
    {
        public static void CreateIfNotExist(BookEntity be)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=sattarcloudlibrary;AccountKey=0VZqOhtYFlTgkOvnvOV4uVjYN735yT74QDEM0OSc+3jTJWvocp/A8ZcNIxyxVJ07jdUIXI0Ckt1wbsL/Koco0Q==;");
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("cadlibrarytable");
            table.CreateIfNotExists();

            TableOperation insertOperation = TableOperation.Insert(be);
            table.Execute(insertOperation);
        }
    }
}
