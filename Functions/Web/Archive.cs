using Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using PocketSharp;
using System.Threading.Tasks;

namespace Functions.Web
{
    public static class Archive
    {
        [FunctionName("Archive")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequest req,
            ExecutionContext context)
        {
            Config _config = new ConfigBuilder(context.FunctionAppDirectory).Build();

            string articleId = req.Query["articleId"];
            string userHash = req.Query["token"];

            if (articleId == null || userHash == null)
            {
                return new BadRequestObjectResult("no token or articleId");
            }

            var userService = UserService.BuildUserService(_config.StorageConnectionString);
            var user = await userService.FindUserWithToken(userHash);

            if (user == null)
            {
                return new BadRequestObjectResult("invalid user hash");
            }

            var pocketClient = new PocketClient(_config.PocketConsumerKey, user.AccessCode);
            var article = await pocketClient.Get(articleId);

            if (article == null)
            {
                return new BadRequestObjectResult("invalid article id");
            }

            var success = await pocketClient.Archive(article);

            //todo have html results
            return success
                ? (ActionResult)new OkObjectResult("Archiving successful")
                : new BadRequestObjectResult("Something went wrong");
        }
    }
}