using System;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using System.IO;
using System.Drawing.Imaging;
using System.IO.Pipelines;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using System.Net;
using System.Security.Policy;

namespace OA.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ImageHelper:ControllerBase
	{
		public readonly OfficialAccountApi _accountHelper;

		public ImageHelper(SqlServerContext db, IConfiguration config)
		{
			_accountHelper = new OfficialAccountApi(db, config);
		}

		[HttpGet]
		public async Task GetImage(string url)
		{
			url = Util.UrlDecode(url);
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "GET";
            HttpWebResponse res = (HttpWebResponse)req.GetResponse();
            Stream sImg = res.GetResponseStream();

			Image<Rgba32> img = await Image.LoadAsync<Rgba32>(sImg);
			string str = img.ToBase64String(img.Metadata.DecodedImageFormat);
			string base64Str = str.Split(',')[1];
			byte[] bArr = Convert.FromBase64String(base64Str);

			Response.ContentType = img.Metadata.DecodedImageFormat.MimeTypes.ToArray()[0].Trim();
			PipeWriter pw = Response.BodyWriter;
			Stream s = pw.AsStream();
			for (int i = 0; i < bArr.Length; i++)
			{
				s.WriteByte(bArr[i]);
			}
			s.Close();
			sImg.Close();
			res.Close();
			req.Abort();
        }

		[HttpGet]
		public async Task GetStaticPoster(string posterUrl, string scene, int x, int y, int width)
		{

			string qrCodeUrl = "../../OfficalAccountApi/ShowQrCodeStatic?scene=" + scene;
            HttpWebRequest reqQr = (HttpWebRequest)WebRequest.Create(qrCodeUrl);
            reqQr.Method = "GET";
            HttpWebResponse resQr = (HttpWebResponse)reqQr.GetResponse();
            Stream sQr = resQr.GetResponseStream();
			Image<Rgba32> imgQr = await Image.LoadAsync<Rgba32>(sQr);
			imgQr.Mutate(x => x.Resize(width, width));

			HttpWebRequest reqPoster = (HttpWebRequest)WebRequest.Create(posterUrl);
			reqPoster.Method = "GET";
			HttpWebResponse resPoster = (HttpWebResponse)reqPoster.GetResponse();
			Stream sPoster = resPoster.GetResponseStream();
            Image<Rgba32> imgPoster = await Image.LoadAsync<Rgba32>(sPoster);
			//imgPoster.Mutate()
			imgPoster.Mutate(x => x.DrawImage(imgQr, 100));
			string str = imgPoster.ToBase64String(imgPoster.Metadata.DecodedImageFormat);
            string base64Str = str.Split(',')[1];
            byte[] bArr = Convert.FromBase64String(base64Str);

            Response.ContentType = imgPoster.Metadata.DecodedImageFormat.MimeTypes.ToArray()[0].Trim();
            PipeWriter pw = Response.BodyWriter;
            Stream s = pw.AsStream();
            for (int i = 0; i < bArr.Length; i++)
            {
                s.WriteByte(bArr[i]);
            }
            s.Close();

        }
	}
}

