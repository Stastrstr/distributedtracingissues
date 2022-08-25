using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FuncDurableTrace
{
    public class SimpleOrchestration
    {
        [FunctionName(nameof(SimpleOrchestrator))]
        public static async Task<List<string>> SimpleOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            List<string> outputs = new()
            {

                // Replace "hello" with the name of your Durable Activity Function.
                await context.CallActivityAsync<string>(nameof(SayHello), "Tokyo"),
                await context.CallActivityAsync<string>(nameof(SayHello), "Seattle"),
                await context.CallActivityAsync<string>(nameof(SayHello), "London"),
                await context.CallActivityAsync<string>(nameof(SayHello), "London2")
            };

            await context.WaitForExternalEvent("NeverRaisingEvent", TimeSpan.FromSeconds(2));

            return outputs;
        }

        [FunctionName(nameof(SayHello))]
        public static string SayHello([ActivityTrigger] string name, ILogger log)
        {
            log.LogInformation($"Saying hello to {name}.");

            if(Activity.Current != null)
            {
                // trying to add custom dimensions to this item.

                Activity.Current.AddTag("Name", name);
            }

            if (name == "London2") throw new InvalidOperationException();

            return $"Hello {name}!";
        }

        [FunctionName(nameof(HttpStart_SimpleOrchestration))]
        public static async Task<IActionResult> HttpStart_SimpleOrchestration(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            string instanceId = await starter.StartNewAsync(nameof(SimpleOrchestrator), null);
            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}
