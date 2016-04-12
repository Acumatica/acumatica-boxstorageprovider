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
            // We use a separate connection to ensure that the token gets persisted to the DB, regardless of any transaction rollback.
            // It could potentially happen that the token needs to get refreshed while a file is uploaded, but that this upload ultimately gets rolled back due to another
            // error later during the caller graph persisting process. If we use the current connection scope we have no control over this update.
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
            BoxUserTokens currentUser = GetCurrentUser();
            var graph = PXGraph.CreateInstance<UserProfile>();

            if (currentUser != null)
            {
                //Clear out stored token if any.
                using (new PXConnectionScope())
                {

                    currentUser = PXCache<BoxUserTokens­>.CreateCopy(currentUser);
                    currentUser.AccessToken = null;
                    currentUser.RefreshToken = null;
                    this.Caches[typeof(BoxUserTokens)].Update(currentUser);
                    this.Caches[typeof(BoxUserTokens)].Persist(PXDBOperation.Update);
                }

                graph.User.Current = currentUser;
            }

            //TODO: Ideally we would like to show a nice message to the user before we send him there.
            throw new PXRedirectRequiredException(graph, Messages.BoxUserNotFound);
        }
    }
}
