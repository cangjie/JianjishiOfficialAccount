using System;
using System.Xml;
using Microsoft.Extensions.Configuration;
using OA;
using OA.Models;


namespace OA.Controllers.Api
{
    public class OfficialAccountReply
    {
        private readonly SqlServerContext _db;

        private readonly IConfiguration _config;

        private readonly Settings _settings;

        private readonly OARecevie _message;

        public OfficialAccountReply(SqlServerContext context,
            IConfiguration config, OARecevie message)
        {
            _db = context;
            _config = config;
            _settings = Settings.GetSettings(_config);
            _message = message;
        }

        public string Reply()
        {
            string retStr = "success";
            if (_message.MsgType.Trim().ToLower().Equals("text"))
            {
                switch (_message.Content.Trim().ToLower())
                {
                    case "海报":
                        retStr = GetPoster().InnerXml.Trim();
                        break;
                    case "1":
                        retStr = GetImageMessage("6saVwTsGr7hh8G_dlZdVbFKyZo5dizp0Q7_N0kaPa1oj-XXNGzEaRAtlcyOWImrE").InnerXml.Trim();
                        break;
                    case "2":
                        retStr = GetImageMessage("6saVwTsGr7hh8G_dlZdVbIwjpc5QZz7L4wnb3f4CSp0YYV8IF__i3LSrIBIWJStb").InnerXml.Trim();
                        break;
                    default:
                        retStr = "success";
                        break;
                }
            }
            return retStr.Trim();
        }

        public XmlDocument GetPoster()
        {
            XmlDocument xmlD = new XmlDocument();
            xmlD.LoadXml("<xml>"
                + "<ToUserName><![CDATA[" + _message.FromUserName.Trim() + "]]></ToUserName>"
                + "<FromUserName ><![CDATA[" + _settings.originalId.Trim() + "]]></FromUserName>"
                + "<CreateTime >" + Util.GetLongTimeStamp(DateTime.Now) + "</CreateTime>"
                + "<MsgType><![CDATA[text]]></MsgType>"
                + "<Content><![CDATA[<a href=\"http://weixin.luqinwenda.com/service/3\" >点击查看海报</a>]]></Content>"
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
