using Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Functions.Web
{
    public static class Report
    {
        [FunctionName("Report")]
        public async static Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]HttpRequest req,
            TraceWriter log,
            ExecutionContext context)
        {
            log.Info("C# HTTP trigger function processed a request.");

            var reportedUrlRow = new ReportedUrl
            {
                PartitionKey = "reportedUrl",
                RowKey = Guid.NewGuid().ToString(),
                Url = req.Query["url"]
            };

            Config config = new ConfigBuilder(context.FunctionAppDirectory).Build();

            await SaveToTable(reportedUrlRow, config.StorageConnectionString);

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent($"<html><body><h1>Thank you for submiting that article!</h1><p>We'll investigate {reportedUrlRow.Url} soon.</p></body></html>");
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/html");
            return response;
        }

        private static async Task SaveToTable(ReportedUrl reportedUrlRow, string storageConnectionString)
        {
            var storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            var cloudTable = tableClient.GetTableReference("ReportedUrls");
            await cloudTable.CreateIfNotExistsAsync();
            var insertOperation = TableOperation.Insert(reportedUrlRow);
            await cloudTable.ExecuteAsync(insertOperation);
        }

        public class ReportedUrl : TableEntity
        {
            public string Url { get; set; }
        }
    }
}