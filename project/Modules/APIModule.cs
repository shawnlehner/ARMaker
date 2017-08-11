using Nancy;
using Nancy.ModelBinding;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace MarkerGenerator.Modules
{
    public class APIModule : NancyModule
    {
        public sealed class GenerateRequestModelV1
        {
            public int? Id { get; set; }
            public int Size { get; set; }
            public string Label { get; set; }
            public MarkerType Type { get; set; }
            public bool Download { get; set; }

            public GenerateRequestModelV1()
            {
                Type = MarkerType.Vuforia;
                Size = 1024;
            }
        }

        private static MarkerGenerator mg = new MarkerGenerator();

        public APIModule()
        {
            Post["/api/v1/id"] = Get["/api/v1/id"] = parameters =>
            {
                return Response.AsJson(mg.GenerateRandomID());
            };

            Post["/api/v1/generate"] = Get["/api/v1/generate"] = parameters =>
            {
                GenerateRequestModelV1 req = this.Bind<GenerateRequestModelV1>();

                if (req.Size > 2048) req.Size = 2048;

                MarkerData m = mg.CreateMarker(req.Id, req.Size, req.Label, req.Type);

                using (Bitmap bmp = m.MarkerImage)
                {
                    MemoryStream ms = new MemoryStream();

                    bmp.Save(ms, ImageFormat.Jpeg);

                    ms.Flush();
                    ms.Seek(0, SeekOrigin.Begin);

                    var res = Response.FromStream(ms, "image/jpeg");

                    if (req.Download) res.Headers.Add("Content-Disposition", "attachment; filename=ar_marker.jpg");

                    return res;
                }
            };
        }
    }
}
