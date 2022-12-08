using System;
using System.Xml;
using Microsoft.Extensions.Configuration;
using OA;
using OA.Models;
using System.Threading.Tasks;

namespace OA.Controllers.Api
{
    public class OfficialAccountReply
    {
        private readonly SqlServerContext _db;

        private readonly IConfiguration _config;

        private readonly Settings _settings;

        private readonly OARecevie _message;

        private string _domain = "";

        public OfficialAccountReply(SqlServerContext context,
            IConfiguration config, OARecevie message)
        {
            _db = context;
            _config = config;
            _settings = Settings.GetSettings(_config);
            _domain = _settings.domainName.Trim();
            _message = message;
        }

        public async Task<string> Reply()
        {
            string retStr = "success";
            string openId = _message.FromUserName.Trim();
            UserController userHelper = new UserController(_db, _config);
            if (_message.MsgType.Trim().ToLower().Equals("text"))
            {
                switch (_message.Content.Trim().ToLower())
                {
                    
                    case "1":
                        retStr = GetImageMessage("6saVwTsGr7hh8G_dlZdVbFKyZo5dizp0Q7_N0kaPa1oj-XXNGzEaRAtlcyOWImrE").InnerXml.Trim();
                        break;
                    case "2":
                        retStr = GetImageMessage("6saVwTsGr7hh8G_dlZdVbIwjpc5QZz7L4wnb3f4CSp0YYV8IF__i3LSrIBIWJStb").InnerXml.Trim();
                        break;
                    case "二维码":
                        int userId = (await userHelper.CheckUser(openId.Trim())).Value;
                        retStr = GetTextMessage("<a href='http://" + _domain.Trim()
                            + "/api/OfficialAccountApi/ShowQrCodeDynamic?expire=259200&scene=freereserve_originuser_"
                            + userId.ToString() + "'  >点击查看二维码</a>").InnerXml.Trim();
                        break;
                    default:
                        retStr = "success";
                        break;
                }
            }
            else
            {
                switch (_message.Event.ToLower().Trim())
                {
                    case "click":
                        switch (_message.EventKey.Trim().ToLower())
                        {
                            case "service":
                                retStr = GetImageMessage("6saVwTsGr7hh8G_dlZdVbFKyZo5dizp0Q7_N0kaPa1oj-XXNGzEaRAtlcyOWImrE").InnerXml.Trim();
                                break;
                            case "shop":
                                retStr = GetImageMessage("6saVwTsGr7hh8G_dlZdVbIwjpc5QZz7L4wnb3f4CSp0YYV8IF__i3LSrIBIWJStb").InnerXml.Trim();
                                break;
                            default:
                                retStr = "success";
                                break;
                        }
                        break;
                    case "subscribe":
                    case "scan":
                        DealSubscribe(_message);
                        break;
                    default:
                        retStr = "success";
                        break;
                }

                
            }
            return retStr.Trim();
        }

        public string DealSubscribe(OARecevie message)
        {
            SendServiceMessageMApp(message.FromUserName.Trim(), "点我预约", "pages/reserve/reserve", "6saVwTsGr7hh8G_dlZdVbFuyMJekFzMY-zsH18dhDZiJnJCGSKg4qD-mK8ed-Tvc");
            return "success";
        }

        public void SendServiceMessageMApp(string openId, string title, string path, string mediaId)
        {
            string appId = _settings.miniAppId.Trim();
            //Util.GetWebContent()
            string postJson = "{\"touser\":\"" + openId + "\",  \"msgtype\":\"miniprogrampage\",    \"miniprogrampage\": {"
                + "\"title\":\"" + title + "\",    \"appid\":\"" + appId + "\", "
                + "\"pagepath\":\"" + path + "\",        \"thumb_media_id\":\"" + mediaId + "\"   }}";
            OfficialAccountApi oaHelper = new OfficialAccountApi(_db, _config);
            string token = oaHelper.GetAccessToken();
            string postUrl = "https://api.weixin.qq.com/cgi-bin/message/custom/send?access_token=" + token.Trim();
            Console.WriteLine(Util.GetWebContent(postUrl, postJson));
        }

        /*
        public void SendServiceMessageText(string openId, string content)
        {
            string postJson = "{\"touser\":\"" + openId + "\",  \"msgtype\":\"text\",    \"text\": {"
                + "\"content\":\"" + content + "\"   }}";
            OfficialAccountApi oaHelper = new OfficialAccountApi(_db, _config);
            string token = oaHelper.GetAccessToken();
            string postUrl = "https://api.weixin.qq.com/cgi-bin/message/custom/send?access_token=" + token.Trim();
            Console.WriteLine(Util.GetWebContent(postUrl, postJson));
        }
        */

        public XmlDocument GetTextMessage(string content)
        {
            XmlDocument xmlD = new XmlDocument();
            xmlD.LoadXml("<xml>"
                + "<ToUserName><![CDATA[" + _message.FromUserName.Trim() + "]]></ToUserName>"
                + "<FromUserName ><![CDATA[" + _settings.originalId.Trim() + "]]></FromUserName>"
                + "<CreateTime >" + Util.GetLongTimeStamp(DateTime.Now) + "</CreateTime>"
                + "<MsgType><![CDATA[text]]></MsgType>"
                //+ "<Content><![CDATA[<a href=\"http://weixin.luqinwenda.com/service/3\" >点击查看海报</a>]]></Content>"
                + "<Content><![CDATA[" + content.Trim() + "]]></Content>"
                + "</xml>");
            return xmlD;
        }





        public XmlDocument GetImageMessage(string mediaId)
        {
            XmlDocument xmlD = new XmlDocument();
            xmlD.LoadXml("<xml>"
                + "<ToUserName><![CDATA[" + _message.FromUserName.Trim() + "]]></ToUserName>"
                + "<FromUserName ><![CDATA[" + _settings.originalId.Trim() + "]]></FromUserName>"
                + "<CreateTime >" + Util.GetLongTimeStamp(DateTime.Now) + "</CreateTime>"
                + "<MsgType><![CDATA[image]]></MsgType>"
                + "<Image>"
                + "<MediaId><![CDATA[" + mediaId + "]]></MediaId>"
                + "</Image>"
                + "</xml>");
            return xmlD;
        }
    }
}
