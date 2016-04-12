using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Data;
using System.IO;
using System.Web.Compilation;
using PX.Common;
using Box.V2.Auth;

namespace PX.SM.BoxStorageProvider
{
    public class UserTokenHandler : PXGraph<UserTokenHandler>
    {
        public BoxUserTokens GetCurrentUser()
        {
            return PXSelect<BoxUserTokens, Where<BoxUserTokens.userID, Equal<Current<AccessInfo.userID>>>>.Select(this);
        }

        public void SessionAuthenticated(object sender, SessionAuthenticatedEventArgs e)
        {
            using (new PXConnectionScope())
            {
                BoxUserTokens currentUser = PXCache<BoxUserTokens>.CreateCopy(GetCurrentUser());
                currentUser.AccessToken = e.Session.AccessToken;
                currentUser.RefreshToken = e.Session.RefreshToken;
                currentUser.RefreshTokenDate = PXTimeZoneInfo.Now;
                this.Caches[typeof(BoxUserTokens)].Update(currentUser);
                this.Caches[typeof(BoxUserTokens)].Persist(PXDBOperation.Update);
            }
        }

        public void SessionInvalidated(object sender, EventArgs e)
        {

        }
    }
}
