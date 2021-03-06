﻿using System.Collections.Generic;
using BinbinDotNetOpenAuth.AspNet;
using BinbinDotNetOpenAuth.AspNet.Clients;
using Microsoft.Web.WebPages.OAuth;

namespace BinbinDotNetOpenAuth.Samples
{
    public static class AuthConfig
    {
        public static void RegisterAuth()
        {
            // To let users of this site log in using their accounts from other sites such as Microsoft, Facebook, and Twitter,
            // you must update this site. For more information visit http://go.microsoft.com/fwlink/?LinkID=252166

            //OAuthWebSecurity.RegisterMicrosoftClient(
            //    clientId: "",
            //    clientSecret: "");

            //OAuthWebSecurity.RegisterTwitterClient(
            //    consumerKey: "",
            //    consumerSecret: "");

            //OAuthWebSecurity.RegisterFacebookClient("", "");
            OAuthWebSecurity.RegisterClient(new WeiboClient("3382544284", "8c91432f093bd16ccba0ca20d529e510"));

            OAuthWebSecurity.RegisterClient(new WeixinClient("wx661a840ecedb554d", "163e891e7b7e144ff4b40bb0eed6b810"),"微信",new Dictionary<string, object>() {});
            OAuthWebSecurity.RegisterGoogleClient();
        }
    }
}