﻿using System;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualBasic;

namespace OA.Models
{
    public class Settings
    {
        public string appId { get; set; } = "";
        public string appSecret { get; set; } = "";
        public string originalId { get; set; } = "";
        public string token { get; set; } = "";
        public string domainName { get; set; } = "";
        public string miniAppId { get; set; } = "";

        public static Settings GetSettings(IConfiguration config)
        {
            IConfiguration settings = config.GetSection("Settings");
            
            var appId = settings.GetSection("AppId").Value;
            var appSecret = settings.GetSection("AppSecret").Value;
            var originalId = settings.GetSection("OriginalId").Value;
            var token = settings.GetSection("token").Value;
            var domainName = settings.GetSection("DomainName").Value;
            var miniAppId = settings.GetSection("MiniAppId").Value;
            return new Settings()
            {
                appId = appId == null? "" : appId.Trim(),
                appSecret = appSecret == null ? "" : appSecret.Trim(),
                originalId = originalId == null ? "" : originalId.Trim() ,
                token = token == null ? "" :token.Trim(),
                domainName = domainName == null ? "" : domainName.Trim(),
                miniAppId = miniAppId == null ? "" : miniAppId.Trim()
            };
        }
    }
}
