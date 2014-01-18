using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using DotNetOpenAuth.AspNet.Clients;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BinbinDotNetOpenAuth.AspNet.Clients
{
    public class DoubanClient : OAuth2Client
    {
        #region Constants and Fields

        /// <summary>
        ///     The authorization endpoint.
        /// </summary>
        private const string AuthorizationEndpoint = "https://www.douban.com/service/auth2/auth";

        /// <summary>
        ///     The token endpoint.
        /// </summary>
        private const string TokenEndpoint = "https://www.douban.com/service/auth2/token";

        /// <summary>
        ///     The user info endpoint.
        /// </summary>
        private const string UserInfoEndpoint = "https://api.douban.com/v2/user/~me";

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
        public DoubanClient(string clientId, string clientSecret)
            : this(clientId, clientSecret, new[]
                                           {
                                               "douban_basic_common"
                                           })
        {
        }

        /// <summary>
        ///     Creates a new Google OAuth2 client.
        /// </summary>
        /// <param name="clientId">The Google Client Id</param>
        /// <param name="clientSecret">The Google Client Secret</param>
        /// <param name="requestedScopes">One or more requested scopes, passed without the base URI.</param>
        public DoubanClient(string clientId, string clientSecret, params string[] requestedScopes)
            : base("douban")
        {
            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new ArgumentNullException("clientId");
            }

            if (string.IsNullOrWhiteSpace(clientSecret))
            {
                throw new ArgumentNullException("clientSecret");
            }

            if (requestedScopes == null)
            {
                throw new ArgumentNullException("requestedScopes");
            }

            if (requestedScopes.Length == 0)
            {
                throw new ArgumentException("One or more scopes must be requested.", "requestedScopes");
            }

            this._clientId = clientId;
            this._clientSecret = clientSecret;
            this._requestedScopes = requestedScopes;
        }

        protected override Uri GetServiceLoginUrl(Uri returnUrl)
        {
            IEnumerable<string> scopes = this._requestedScopes;
            string state = string.IsNullOrEmpty(returnUrl.Query) ? string.Empty : returnUrl.Query.Substring(1);

            return UriHelper.BuildUri(AuthorizationEndpoint, new NameValueCollection
                                                             {
                                                                 {"response_type", "code"},
                                                                 {"client_id", this._clientId},
                                                                 {"scope", string.Join(" ", scopes)},
                                                                 {"redirect_uri", returnUrl.GetLeftPart(UriPartial.Path)},
                                                                 {"state", state},
                                                             });
        }

        protected override IDictionary<string, string> GetUserData(string accessToken)
        {
            Uri uri = UriHelper.BuildUri(UserInfoEndpoint, new NameValueCollection
                                                           {
                                                               {"access_token", accessToken},
                                                           });

            var webRequest = (HttpWebRequest) WebRequest.Create(uri);

            using (WebResponse webResponse = webRequest.GetResponse())
            {
                using (Stream stream = webResponse.GetResponseStream())
                {
                    if (stream == null)
                    {
                        return null;
                    }

                    using (var textReader = new StreamReader(stream))
                    {
                        string json = textReader.ReadToEnd();
                        var user = JsonConvert.DeserializeObject<DoubanUserData>(json);
                        var extraData = new Dictionary<string, string>
                                        {
                                            {"id", user.id},
                                            {"name", user.name},
                                        };
                        return extraData;
                    }
                }
            }
        }

        protected override string QueryAccessToken(Uri returnUrl, string authorizationCode)
        {
            var collection = new NameValueCollection
                             {
                                 {"grant_type", "authorization_code"},
                                 {"code", authorizationCode},
                                 {"client_id", this._clientId},
                                 {"client_secret", this._clientSecret},
                                 {"redirect_uri", returnUrl.GetLeftPart(UriPartial.Path)},
                             };
            string response = UriHelper.OAuthPost(TokenEndpoint, collection);
            if (response == null)
            {
                return null;
            }
            JObject json = JObject.Parse(response);
            //HttpContext.Current.Session["uid"] = json.Value<string>("douban_user_id");
            return json.Value<string>("access_token");
        }
    }
}