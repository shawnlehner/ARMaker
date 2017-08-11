using Nancy;
using Nancy.Bootstrapper;
using Nancy.TinyIoc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace MarkerGenerator
{
    public sealed class Bootstrapper : DefaultNancyBootstrapper
    {
        protected override void RequestStartup(TinyIoCContainer container, IPipelines pipelines, NancyContext context)
        {
            base.RequestStartup(container, pipelines, context);

            //CORS Enable
            pipelines.AfterRequest.AddItemToEndOfPipeline((ctx) =>
            {
                ctx.Response.WithHeader("Access-Control-Allow-Origin", "*")
                    .WithHeader("Access-Control-Allow-Methods", "POST,GET,OPTIONS")
                    .WithHeader("Access-Control-Allow-Headers", "Accept, Origin, Content-type");

                if (ctx.Request.Method == "OPTIONS")
                {
                    // handle CORS preflight request

                    ctx.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, OPTIONS";

                    if (ctx.Request.Headers.Keys.Contains("Access-Control-Request-Headers"))
                    {
                        ctx.Response.Headers["Access-Control-Allow-Headers"] = string.Join(", ", ctx.Request.Headers["Access-Control-Request-Headers"]);
                    }
                }

            });

        }
    }
}
