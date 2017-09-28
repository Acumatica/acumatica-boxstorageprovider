using PX.Common;
using PX.Data;
using System;
using System.Collections;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;

namespace PX.SM.BoxStorageProvider
{
    public class UserProfile : PXGraph<UserProfile>
    {
        public PXSave<BoxUserTokens> Save;
        public PXSelect<BoxUserTokens, Where<BoxUserTokens.userID, Equal<Current<AccessInfo.userID>>>> User;
        public PXFilter<BoxAuthInfo> AuthInfo;

        public PXAction<BoxUserTokens> login;
        [PXUIField(DisplayName = "Login to Box")]
        [PXButton(ImageKey = "LinkWB")]
        public virtual IEnumerable Login(PXAdapter adapter)
        {
            Actions.PressSave();

            string state = "acumaticaUrl=" + HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority) + System.Web.VirtualPathUtility.ToAbsolute("~/Pages/SM/SM202615.aspx") +
                "&userID=" + this.User.Current.UserID.ToString();

            string authUrl = "https://www.box.com/api/oauth2/authorize?response_type=code" +
                "&client_id=" + BoxUtils.ClientID +
                "&state=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(state));

            throw new PXRedirectToUrlException(authUrl, PXBaseRedirectException.WindowMode.NewWindow, "");
        }
        
        public PXAction<BoxUserTokens> completeAuthentication;
        [PXButton(ImageKey = "LinkWB")]
        public virtual IEnumerable CompleteAuthentication(PXAdapter adapter)
        {
            PXLongOperation.StartOperation(this, delegate
            {
                User.Cache.Clear();
                var tokenHandler = PXGraph.CreateInstance<UserTokenHandler>();
                BoxUserTokens boxUser = PXCache<BoxUserTokens>.CreateCopy(User.Select());
                var userInfo = BoxUtils.GetUserInfo(tokenHandler).Result;
                boxUser.BoxUserID = userInfo.Id;
                boxUser.BoxEmailAddress = userInfo.Login;
                User.Update(boxUser);
                Actions.PressSave();
            });

            return adapter.Get();
        }

        public virtual void BoxUserTokens_RowSelected(PXCache cache, PXRowSelectedEventArgs e)
        {
            var user = (BoxUserTokens) e.Row;
            if (string.IsNullOrEmpty(user.BoxUserID))
            {
                user.UserStatus = PXLocalizer.Localize(Messages.NotConfigured);
            }
            else
            {
                if (string.IsNullOrEmpty(user.AccessToken))
                {
                    user.UserStatus = PXLocalizer.Localize(Messages.Expired);
                }
                else
                {
                    user.UserStatus = PXLocalizer.Localize(Messages.Configured);
                }
            }

            Save.SetVisible(false);
        }

        public virtual void BoxUserTokens_RefreshTokenDate_FieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
        {
            var userToken = e.Row as BoxUserTokens;
            if (userToken != null && userToken.RefreshTokenDate.HasValue)
            {
                e.ReturnValue = PXTimeZoneInfo.ConvertTimeFromUtc(userToken.RefreshTokenDate.Value, PXContext.PXIdentity.TimeZone);
            }
        }

        [Serializable]
        public class BoxAuthInfo : IBqlTable
        {
            public abstract class authTicket : IBqlField { }
            public virtual string AuthTicket { get; set; }
        }
    }
}
