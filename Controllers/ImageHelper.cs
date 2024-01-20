using System;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using System.IO;
using System.Drawing.Imaging;
using System.IO.Pipelines;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using System.Net;
namespace OA.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImageHelper:ControllerBase
	{
		public ImageHelper()
		{

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
	}
}

