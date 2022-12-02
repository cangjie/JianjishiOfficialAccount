using System;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Xml;
using Microsoft.Extensions.Configuration;
using OA.Models;
using System.Security.Cryptography;
using Newtonsoft.Json;
using System.Web;
using System.Runtime;
using OA.Controllers.Api;
using System.IO.Pipelines;
using System.Net;
//using MiniApp.Models;
using System.Threading.Tasks;
namespace OA.Controllers
{
    [Route("api/[controller]/[Action]")]
    [ApiController]
    public class OfficialAccountApi : ControllerBase
    {
        private readonly SqlServerContext _db;

        private readonly IConfiguration _config;

        private readonly Settings _settings;

        public OfficialAccountApi(SqlServerContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
            _settings = Settings.GetSettings(_config);
        }

        [HttpGet]
        public ActionResult<string> ShowTime()
        {
            DateTime now = DateTime.Now;
            
            return now.ToUniversalTime().ToString() + " " +  now.ToLocalTime().ToString() + " " + Util.GetLongTimeStamp(now);
        }

        [HttpGet]
        public async Task<ActionResult<string>> PushMessage(string signature,
            string timestamp, string nonce, string echostr)
        {
            return echostr.Trim();
        }

        [NonAction]
        public void ShowQrCode(string ticket)
        {
            
            string imgUrl = "https://mp.weixin.qq.com/cgi-bin/showqrcode?ticket=" + ticket.Trim();
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(imgUrl);
            HttpWebResponse res = (HttpWebResponse)req.GetResponse();
            byte[] buf = new byte[1024 * 1024 * 100];
            Stream s = res.GetResponseStream();
            int i = s.ReadByte();
            int j = 0;
            while (i >= 0)
            {
                buf[j] = (byte)i;
                i = s.ReadByte();
                j++;
            }
            s.Close();
            res.Close();
            req.Abort();
            byte[] buff = new byte[j];
            for (int k = 0; k < j; k++)
            {
                buff[k] = buf[k];
            }
            Response.ContentType = "image/jpeg";
            PipeWriter pw = Response.BodyWriter;
            Stream sOut = pw.AsStream();
            for (int k = 0; k < buff.Length; k++)
            {
                sOut.WriteByte(buff[k]);
                
            }
            sOut.Close();
        }
        
        [HttpPost]
        public async  Task<ActionResult<string>> SendTextServiceMessage([FromQuery]string unionId)
        {
            unionId = Util.UrlDecode(unionId);
            User user = _db.user.Where(u => u.oa_union_id.Trim().Equals(unionId.Trim())).First();
            OAUser oaUser = _db.oAUser.Where(u => u.user_id == user.id).First();
           // string content = "";
            Stream stream = Request.Body;
            StreamReader sr = new StreamReader(stream);
            string content = (await sr.ReadToEndAsync());


            string postJson = "{\"touser\":\"" + oaUser.open_id.Trim() + "\",  \"msgtype\":\"text\",    \"text\": {"
                + "\"content\":\"" + content + "\"   }}";
            //OfficialAccountApi oaHelper = new OfficialAccountApi(_db, _config);
            string token = GetAccessToken();
            string postUrl = "https://api.weixin.qq.com/cgi-bin/message/custom/send?access_token=" + token.Trim();
            return (Util.GetWebContent(postUrl, postJson));
        }
        
        [HttpGet]
        public void ShowQrCodeStatic(string scene)
        {
            string token = GetAccessToken().Trim();
            string url = "https://api.weixin.qq.com/cgi-bin/qrcode/create?access_token=" + token.Trim();
            string postData = "{\"action_name\": \"QR_LIMIT_STR_SCENE\", \"action_info\": {\"scene\": {\"scene_str\": \"" + scene.Trim() + "\"}}}";
            string jsonStr = Util.GetWebContent(url, postData).Trim();
            Newtonsoft.Json.Linq.JToken json = (Newtonsoft.Json.Linq.JToken)JsonConvert.DeserializeObject(jsonStr);
            ShowQrCode(json["ticket"].ToString().Trim());
            //Util.GetWebContent()
        }

        [HttpGet]
        public void ShowQrCodeDynamic(string scene, int expire)
        {
            string token = GetAccessToken().Trim();
            string url = "https://api.weixin.qq.com/cgi-bin/qrcode/create?access_token=" + token.Trim();
            string postData = "{\"expire_seconds\": " + expire.ToString() + ", \"action_name\": \"QR_STR_SCENE\", \"action_info\": {\"scene\": {\"scene_str\": \"" + scene.Trim() + "\"}}}";
            string jsonStr = Util.GetWebContent(url, postData).Trim();
            Newtonsoft.Json.Linq.JToken json = (Newtonsoft.Json.Linq.JToken)JsonConvert.DeserializeObject(jsonStr);
            ShowQrCode(json["ticket"].ToString().Trim());
        }
       

        [HttpPost]
        public async Task<ActionResult<string>> PushMessage([FromQuery] string signature,
            [FromQuery] string timestamp, [FromQuery] string nonce)
        {
            string[] validStringArr = new string[] { _settings.token.Trim(), timestamp.Trim(), nonce.Trim() };
            Array.Sort(validStringArr);
            string validString = String.Join("", validStringArr);
            SHA1 sha = SHA1.Create();
            ASCIIEncoding enc = new ASCIIEncoding();
            byte[] bArr = enc.GetBytes(validString);
            bArr = sha.ComputeHash(bArr);
            string validResult = "";
            for (int i = 0; i < bArr.Length; i++)
            {
                validResult = validResult + bArr[i].ToString("x").PadLeft(2, '0');
            }
            if (validResult != signature)
            {
                return NoContent();
            }
            string body = "";
            var stream = Request.Body;
            if (stream != null)
            {

                using (var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, true))
                {
                    body = await reader.ReadToEndAsync();

                    string path = $"{Environment.CurrentDirectory}";
                    string dateStr = DateTime.Now.Year.ToString() + DateTime.Now.Month.ToString().PadLeft(2, '0')
                        + DateTime.Now.Day.ToString().PadLeft(2, '0');
                    if (path.StartsWith("/"))
                    {
                        path = path + "/";
                    }
                    else
                    {
                        path = path + "\\";
                    }
                    path = path + "wechat_post_" + dateStr + ".txt";
                    using (StreamWriter fw = new StreamWriter(path, true))
                    {
                        fw.WriteLine(body.Trim());
                        fw.Close();
                    }
                }

            }
            try
            {
                XmlDocument xmlD = new XmlDocument();
                xmlD.LoadXml(body);
                XmlNode root = xmlD.SelectSingleNode("//xml");

                string eventStr = "";
                string eventKey = "";
                string content = "";
                string msgId = "";
                string msgType = root.SelectSingleNode("MsgType").InnerText.Trim();

                if (msgType.Trim().Equals("event"))
                {
                    eventStr = root.SelectSingleNode("Event").InnerText.Trim();
                    eventKey = root.SelectSingleNode("EventKey").InnerText.Trim();
                }
                else
                {
                    content = root.SelectSingleNode("Content").InnerText.Trim();
                    msgId = root.SelectSingleNode("MsgId").InnerText.Trim();
                    msgType = root.SelectSingleNode("MsgType").InnerText.Trim();
                }

                OARecevie msg = new OARecevie()
                {
                    id = 0,
                    ToUserName = root.SelectSingleNode("ToUserName").InnerText.Trim(),
                    FromUserName = root.SelectSingleNode("FromUserName").InnerText.Trim(),
                    CreateTime = root.SelectSingleNode("CreateTime").InnerText.Trim(),
                    MsgType = msgType,
                    Event = eventStr,
                    EventKey = eventKey,
                    MsgId = msgId,
                    Content = content

                };
                await _db.oARecevie.AddAsync(msg);
                await _db.SaveChangesAsync();
                try
                {
                    await (new UserController(_db, _config)).CheckUser(msg.FromUserName.Trim());
                }
                catch
                {

                }
                //return ReturnMessage(msg);

                OfficialAccountReply reply = new OfficialAccountReply(_db, _config, msg);

                return (await reply.Reply()).Trim();

            }
            catch
            {

            }
            
            return "success";
        }

        [HttpGet]
        public async Task<ActionResult<string>> TestReplyAsync(int id)
        {
            OARecevie msg = await _db.oARecevie.FindAsync(id);
            OfficialAccountReply reply = new OfficialAccountReply(_db, _config, msg);
            return await reply.Reply();
        }

        [HttpGet]
        public void RefreshAccessToken()
        {
            GetAccessToken();
        }

        

        [NonAction]
        public string GetAccessToken()
        {
            string tokenFilePath = $"{Environment.CurrentDirectory}";
            tokenFilePath = tokenFilePath + "/access_token.official_account";
            string token = "";
            string tokenTime = Util.GetLongTimeStamp(DateTime.Parse("1970-1-1"));
            string nowTime = Util.GetLongTimeStamp(DateTime.Now);
            bool fileExists = false;
            if (System.IO.File.Exists(tokenFilePath))
            {
                fileExists = true;
                using (StreamReader sr = new StreamReader(tokenFilePath))
                {
                    try
                    {
                        token = sr.ReadLine();
                    }
                    catch
                    {

                    }
                    try
                    {
                        tokenTime = sr.ReadLine();
                    }
                    catch
                    {

                    }
                    sr.Close();
                }
                long timeDiff = long.Parse(nowTime) - long.Parse(tokenTime);
                TimeSpan ts = new TimeSpan(0, 0, 0, 0, (int)timeDiff);
                //TimeSpan ts = new TimeSpan()
                if (ts.TotalSeconds > 3600)
                {
                    token = "";
                    if (fileExists)
                    {
                        System.IO.File.Delete(tokenFilePath);
                    }
                }
                else
                {
                    //return tokenFilePath;
                    return token;
                }
            }
            string getTokenUrl = "https://api.weixin.qq.com/cgi-bin/token?grant_type=client_credential&appid="
                + _settings.appId.Trim() + "&secret=" + _settings.appSecret.Trim();
            try
            {
                string ret = Util.GetWebContent(getTokenUrl);
                AccessToken at = JsonConvert.DeserializeObject<AccessToken>(ret);
                if (!at.access_token.Trim().Equals(""))
                {
                    System.IO.File.AppendAllText(tokenFilePath, at.access_token + "\r\n" + nowTime);
                    //return tokenFilePath.Trim();
                    return at.access_token.Trim();
                    //return "";
                }
                else
                {
                    return "";
                }
            }
            catch(Exception err)
            {
                return err.ToString();
            }

        }

        
        [HttpGet]
        public void PageAuth(string callBackUrl)
        {
            //var t = _db.oaPageAuthState.Find(1);
            OAPageAuthState state = new OAPageAuthState()
            {
                id = 0,
                redirect_url = callBackUrl,
                callbacked = 0
            };
            _db.oaPageAuthState.Add(state);
            _db.SaveChanges();
            string redirectUrl = Request.Scheme.Trim() + "://" + Request.Host.ToString()
                + "/service" + Request.Path.ToString() + "Callback";
            string url = "https://open.weixin.qq.com/connect/oauth2/authorize?appid=" + _settings.appId.Trim()
                + "&redirect_uri=" + Util.UrlEncode(redirectUrl)
                + "&response_type=code&scope=snsapi_base&state=" + state.id.ToString() + "#wechat_redirect";
            Response.Redirect(url);
        }

        [HttpGet]
        public async Task<ActionResult<string>> PageAuthCallBack(string code, string state)
        {
            /*
            if (!Util.isDev)
            {
                return code.Trim();
            }
            */
            int stateId = 0;
            try
            {
                stateId = int.Parse(state);
            }
            catch
            {

            }

            string jsonStr = Util.GetWebContent("https://api.weixin.qq.com/sns/oauth2/access_token?appid="
                + _settings.appId.Trim() + "&secret=" + _settings.appSecret.Trim() + "&code="
                + code.Trim() + "&grant_type=authorization_code");
            UserToken token = JsonConvert.DeserializeObject<UserToken>(jsonStr);
            if (token.access_token.Trim().Equals(""))
            {
                return "";
            }

            UserController userController = new UserController(_db, _config);
            userController.SetToken(token.access_token.Trim(), token.openid.Trim(), token.expires_in);
            OAPageAuthState pState = await _db.oaPageAuthState.FindAsync(stateId);
            if (pState != null && pState.callbacked == 0)
            {
                pState.callbacked = 1;
                _db.Entry(pState).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                _db.SaveChanges();
                string redirctUrl = pState.redirect_url.Trim();
                if (redirctUrl.IndexOf('?') > 0)
                {
                    redirctUrl = redirctUrl + "&";
                }
                else
                {
                    redirctUrl = redirctUrl + "?";
                }
                redirctUrl = redirctUrl + "token=" + Util.UrlEncode(token.access_token.Trim());
                Response.Redirect(redirctUrl);
            }
            return token.access_token.Trim();
        }

        [HttpGet]
        public ActionResult<string> GetUnionId(string openId)
        {
            string token = GetAccessToken().Trim();
            //token = "55_F7LX5DglNN1jPuuiSHHvsKf3oiXNRsgChaJQXRV992QyCk_H1tVo9ygOZn_aTSK02Kg37kAThhgJ9zrAHS51v_4YAhVVfIAFcqex_MvLSzd36TfxTN21Qz5eE9G91Gt36EuBKwD6vQKqPj5BPGUjAEAULZ";

            string getInfoUrl = "https://api.weixin.qq.com/cgi-bin/user/info?access_token="
                + token + "&openid=" + openId.Trim() + "&lang=zh_CN";
            string jsonStr = Util.GetWebContent(getInfoUrl);

            UserInfo info = JsonConvert.DeserializeObject<UserInfo>(jsonStr);

            return info.unionid;
        }

        protected class UserInfo
        {
            public int subscribe = 0;
            public string openid = "";
            public string language = "";
            public long subscribe_time = 0;
            public string unionid = "";
            public string remark = "";
            public int groupid = 0;
            public int[] tagid_list = new int[] { 0, 0 };
            public string subscribe_scene = "";
            public string qr_scene = "";
            public string qr_scene_str = "";
        }
        protected class UserToken
        {
            public string access_token = "";
            public int expires_in = 0;
            public string refresh_token = "";
            public string openid = "";
            public string scope = "";
        }

        protected class AccessToken
        {
            public string access_token = "";
            public int expires_in = 0;

        }
    }

}
