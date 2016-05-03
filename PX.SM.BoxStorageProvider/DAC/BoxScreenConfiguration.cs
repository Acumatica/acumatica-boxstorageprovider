namespace PX.SM.BoxStorageProvider
{
    using PX.Data;

    [System.Serializable()]
	public class BoxScreenConfiguration : IBqlTable
    {
		#region ScreenID
		public abstract class screenID : IBqlField
        {
		}
		protected string _ScreenID;
		[PXDBString(8, IsKey = true)]
		[PXDefault()]
		[PXUIField(DisplayName = "Screen")]
		public virtual string ScreenID
		{
			get
			{
				return _ScreenID;
			}
			set
			{
                _ScreenID = value;
			}
		}
		#endregion
		#region LineCntr
		public abstract class lineCntr : IBqlField
        {
		}
		protected int? _LineCntr;
		[PXDBInt()]
		[PXDefault(0)]
		[PXUIField(DisplayName = "LineCntr")]
		public virtual int? LineCntr
		{
			get
			{
				return _LineCntr;
			}
			set
			{
                _LineCntr = value;
			}
		}
		#endregion
	}
}
