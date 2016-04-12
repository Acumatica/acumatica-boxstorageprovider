using System;
using PX.Data;

namespace PX.SM.BoxStorageProvider
{
    [Serializable]
    public class BoxUserTokens : IBqlTable
    {
        public abstract class userStatus : IBqlField { }
        [PXUIField(DisplayName = "User Status", IsReadOnly = true)]
        public virtual string UserStatus { get; set; }

        public abstract class userID : IBqlField { }
        [PXUIField(DisplayName = "User ID", Visible = false)]
        [PXDBGuid(false, IsKey = true)]
        [PXDefault(typeof(AccessInfo.userID))]
        public virtual Guid? UserID { get; set; }

        public abstract class accessToken : IBqlField { }
        [PXUIField(DisplayName = "Box Access Token", IsReadOnly = true, Visible = false)]
        [PXDBCryptString(255)]
        public virtual string AccessToken { get; set; }

        public abstract class refreshToken : IBqlField { }
        [PXDBCryptString(255)]
        [PXUIField(DisplayName = "Box Refresh Token", IsReadOnly = true, Visible = false)]
        public virtual string RefreshToken { get; set; }

        public abstract class refreshTokenDate : IBqlField { }
        [PXDBDateAndTime(PreserveTime = true, UseSmallDateTime = false, UseTimeZone = false)]
        [PXUIField(DisplayName = "Last Refresh Token", IsReadOnly = true, Visibility = PXUIVisibility.Visible)]
        public virtual DateTime? RefreshTokenDate { get; set; }

        public abstract class boxUserID : IBqlField { }
        [PXDBString(255)]
        [PXUIField(DisplayName = "Box User ID", IsReadOnly = true)]
        public virtual string BoxUserID { get; set; }

        public abstract class boxEmailAddress : IBqlField { }
        [PXDBString(255)]
        [PXUIField(DisplayName = "Box E-mail Address", IsReadOnly = true)]
        public virtual string BoxEmailAddress { get; set; }
    }
}
