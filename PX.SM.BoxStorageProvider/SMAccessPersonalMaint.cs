using System;
using PX.Objects;
using PX.Data;

namespace PX.SM.BoxStorageProvider
{
  
  public class SMAccessPersonalMaint_Extension : PXGraphExtension<SMAccessPersonalMaint>
  {
        public PXAction<Users> redirectToBoxUserProfile;
        [PXUIField(DisplayName = Messages.BoxUserProfile)]
        [PXButton()]
        public void RedirectToBoxUserProfile()
        {
            var graph = PXGraph.CreateInstance<UserProfile>();
            PXRedirectHelper.TryRedirect(graph, PXRedirectHelper.WindowMode.Same);
        }
  }
}