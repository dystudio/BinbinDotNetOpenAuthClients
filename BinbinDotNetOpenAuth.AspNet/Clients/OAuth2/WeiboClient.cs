﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Web;
using BinbinDotNetOpenAuth.AspNet.Clients.OAuth2;
using DotNetOpenAuth.AspNet.Clients;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BinbinDotNetOpenAuth.AspNet
{
    /// <summary>
    ///     A DotNetOpenAuth client for logging in to Google using OAuth2.
    ///     Reference: https://developers.google.com/accounts/docs/OAuth2
    /// </summary>
    public class WeiboClient : OAuth2Client
    {
        #region Constants and Fields

        /// <summary>
        ///     The authorization endpoint.
        /// </summary>
        private const string AuthorizationEndpoint = "https://api.weibo.com/oauth2/authorize";

        /// <summary>
        ///     The token endpoint.
        /// </summary>
        private const string TokenEndpoint = "https://api.weibo.com/oauth2/access_token";

        /// <summary>
        ///     The user info endpoint.
        /// </summary>
        private const string UserInfoEndpoint = "https://api.weibo.com/2/users/show.json";

        /// <summary>
        ///     The _app id.
        /// </summary>
        private readonly string _clientId;

        /// <summary>
        ///     The _app secret.
        /// </summary>
        private readonly string _clientSecret;

        /// <summary>
        ///     The requested scopes.
        /// </summary>
        private readonly string[] _requestedScopes;

        #endregion

        /// <summary>
        ///     Creates a new Google OAuth2 Client, requesting the default "userinfo.profile" and "userinfo.email" scopes.
        /// </summary>
        /// <param name="clientId">The Google Client Id</param>
        /// <param name="clientSecret">The Google Client Secret</param>
        public WeiboClient(string clientId, string clientSecret) : this(clientId, clientSecret, new[] {"email"})
        {
        }

        /// <summary>
        ///     Creates a new Google OAuth2 client.
        /// </summary>
        /// <param name="clientId">The Google Client Id</param>
        /// <param name="clientSecret">The Google Client Secret</param>
        /// <param name="requestedScopes">One or more requested scopes, passed without the base URI.</param>
        public WeiboClient(string clientId, string clientSecret, params string[] requestedScopes) : base("weibo")
        {
            if (string.IsNullOrWhiteSpace(clientId))
                throw new ArgumentNullException("clientId");

            if (string.IsNullOrWhiteSpace(clientSecret))
                throw new ArgumentNullException("clientSecret");

            if (requestedScopes == null)
                throw new ArgumentNullException("requestedScopes");

            if (requestedScopes.Length == 0)
                throw new ArgumentException("One or more scopes must be requested.", "requestedScopes");

            _clientId = clientId;
            _clientSecret = clientSecret;
            _requestedScopes = requestedScopes;
        }

        protected override Uri GetServiceLoginUrl(Uri returnUrl)
        {
            IEnumerable<string> scopes = _requestedScopes;
            string state = string.IsNullOrEmpty(returnUrl.Query) ? string.Empty : returnUrl.Query.Substring(1);

            return BuildUri(AuthorizationEndpoint, new NameValueCollection {{"response_type", "code"}, {"client_id", _clientId}, {"scope", string.Join(" ", scopes)}, {"redirect_uri", returnUrl.GetLeftPart(UriPartial.Path)}, {"state", state},});
        }

        protected override IDictionary<string, string> GetUserData(string accessToken)
        {
            var uid = (string) HttpContext.Current.Session["uid"];
            Uri uri = BuildUri(UserInfoEndpoint, new NameValueCollection {{"access_token", accessToken}, {"uid", uid}});

            var webRequest = (HttpWebRequest) WebRequest.Create(uri);

            using (WebResponse webResponse = webRequest.GetResponse())
            using (Stream stream = webResponse.GetResponseStream())
            {
                if (stream == null)
                    return null;

                using (var textReader = new StreamReader(stream))
                {
                    string json = textReader.ReadToEnd();
                    var user = JsonConvert.DeserializeObject<WeiboUserData>(json);
                    var extraData = new Dictionary<string, string>();
                    extraData.Add("id", user.id);
                    extraData.Add("name", user.name);
                    extraData.Add("screen_name", user.screen_name);
                    return extraData;
                }
            }
        }

        protected override string QueryAccessToken(Uri returnUrl, string authorizationCode)
        {
            NameValueCollection postData = HttpUtility.ParseQueryString(string.Empty);
            postData.Add(new NameValueCollection {{"grant_type", "authorization_code"}, {"code", authorizationCode}, {"client_id", _clientId}, {"client_secret", _clientSecret}, {"redirect_uri", returnUrl.GetLeftPart(UriPartial.Path)},});

            var webRequest = (HttpWebRequest) WebRequest.Create(TokenEndpoint);

            webRequest.Method = "POST";
            webRequest.ContentType = "application/x-www-form-urlencoded";

            using (Stream s = webRequest.GetRequestStream())
            using (var sw = new StreamWriter(s))
                sw.Write(postData.ToString());

            using (WebResponse webResponse = webRequest.GetResponse())
            {
                Stream responseStream = webResponse.GetResponseStream();
                if (responseStream == null)
                    return null;

                using (var reader = new StreamReader(responseStream))
                {
                    string response = reader.ReadToEnd();
                    JObject json = JObject.Parse(response);
                    var uid = json.Value<string>("uid");
                    HttpContext.Current.Session["uid"] = uid;
                    var accessToken = json.Value<string>("access_token");
                    return accessToken;
                }
            }
        }

        private static Uri BuildUri(string baseUri, NameValueCollection queryParameters)
        {
            NameValueCollection q = HttpUtility.ParseQueryString(string.Empty);
            q.Add(queryParameters);
            var builder = new UriBuilder(baseUri) {Query = q.ToString()};
            return builder.Uri;
        }

        /// <summary>
        ///     Google requires that all return data be packed into a "state" parameter.
        ///     This should be called before verifying the request, so that the url is rewritten to support this.
        /// </summary>
        public static void RewriteRequest()
        {
            HttpContext ctx = HttpContext.Current;

            string stateString = HttpUtility.UrlDecode(ctx.Request.QueryString["state"]);
            if (stateString == null || !stateString.Contains("__provider__=google"))
                return;

            NameValueCollection q = HttpUtility.ParseQueryString(stateString);
            q.Add(ctx.Request.QueryString);
            q.Remove("state");

            ctx.RewritePath(ctx.Request.Path + "?" + q);
        }
    }
}