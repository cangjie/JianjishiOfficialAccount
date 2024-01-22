using System;
using System.Xml;
using Microsoft.Extensions.Configuration;
using OA;
using OA.Models;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace OA.Controllers.Api
{
    public class OfficialAccountReply
    {
        private readonly SqlServerContext _db;

        private readonly IConfiguration _config;

        private readonly Settings _settings;

        private readonly OARecevie _message;

        private string _domain = "";

        private readonly ChannelFollowController _channelHelper;

        public OfficialAccountReply(SqlServerContext context,
            IConfiguration config, OARecevie message)
        {
            _db = context;
            _config = config;
            _settings = Settings.GetSettings(_config);
            _domain = _settings.domainName.Trim();
            _message = message;
            _channelHelper = new ChannelFollowController(context, config);


        }

        public async Task<string> Reply()
        {
            string retStr = "success";
            string openId = _message.FromUserName.Trim();
            UserController userHelper = new UserController(_db, _config);
            int userId =  (await userHelper.CheckUser(openId.Trim())).Value;
            User? user = await _db.user.FindAsync(userId);
            MiniUser? mUser = await _db.miniUser.FindAsync(0);
            if (user != null)
            {
                var mUserList = await _db.miniUser.Where(m => m.union_id != null
                    && m.union_id.Trim().Equals(user.oa_union_id)).AsNoTracking().ToListAsync();
                if (mUserList.Count > 0)
                {
                    mUser = mUserList[0];
                }
            }
            
            
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
                        //int userId = (await userHelper.CheckUser(openId.Trim())).Value;
                        retStr = GetTextMessage("<a href='http://" + _domain.Trim()
                            + "/api/OfficialAccountApi/ShowQrCodeDynamic?expire=259200&scene=freereserve_originuser_"
                            + userId.ToString() + "'  >点击查看二维码</a>").InnerXml.Trim();
                        break;
                    case "预约":
                        SendServiceMessageText(openId.Trim(), "预约电话139-0116-2727，或添加下方二维码。");
                        retStr = GetImageMessage("6saVwTsGr7hh8G_dlZdVbIAOuNjYwxmR5ZKOs-txBolkwD8j7iK8sWvdhh1iidmo").InnerXml.Trim();
                        break;
                    case "后台":
                        if (mUser != null && mUser.staff == 1)
                        {
                            retStr = GetTextMessage("<a data-miniprogram-appid=\"" + _settings.miniAppId.Trim() + "\" "
                                + " data-miniprogram-path=\"pages/admin/admin\" >登录后台</a>").InnerXml.Trim();
                        }

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
                        await DealSubscribe(_message);
                        break;
                    default:
                        retStr = "success";
                        break;
                }

                
            }
            return retStr.Trim();
        }

        public async Task<string> DealSubscribe(OARecevie message)
        {
            string[] keyArr = message.EventKey.Split('_');
            int channelUserId = 0;
            int currentUserId = 0;

            for (int i = 0; i < keyArr.Length; i++)
            {
                string[] subKey = keyArr[i].Split('-');
                if (subKey.Length == 2)
                {
                    switch (subKey[0].Trim())
                    {
                        case "userid":
                            try
                            {
                                channelUserId = int.Parse(subKey[1].Trim());
                            }
                            catch
                            {

                            }
                            break;

                        default:
                            break;
                    }
                }

            }
            
            var userList = await _db.oAUser.Where(u => u.open_id.Trim().Equals(message.FromUserName.Trim()))
                .AsNoTracking().ToListAsync();
            if (userList.Count > 0)
            {
                currentUserId = userList[0].user_id;

                await _channelHelper.SetFollow(currentUserId, channelUserId);
            }
            
            SendServiceMessageMApp(message.FromUserName.Trim(), "点我预约", "pages/product/product_list", "6saVwTsGr7hh8G_dlZdVbFuyMJekFzMY-zsH18dhDZiJnJCGSKg4qD-mK8ed-Tvc");


           
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

        
        public void SendServiceMessageText(string openId, string content)
        {
            string postJson = "{\"touser\":\"" + openId + "\",  \"msgtype\":\"text\",    \"text\": {"
                + "\"content\":\"" + content + "\"   }}";
            OfficialAccountApi oaHelper = new OfficialAccountApi(_db, _config);
            string token = oaHelper.GetAccessToken();
            string postUrl = "https://api.weixin.qq.com/cgi-bin/message/custom/send?access_token=" + token.Trim();
            Console.WriteLine(Util.GetWebContent(postUrl, postJson));
        }
        

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
