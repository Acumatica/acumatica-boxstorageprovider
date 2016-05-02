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
            throw new PXRedirectToUrlException("~/Pages/SM/SM202610.aspx", PXLocalizer.Localize(Messages.BoxUserProfile));
        }
  }
}