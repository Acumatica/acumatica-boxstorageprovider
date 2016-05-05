namespace PX.SM.BoxStorageProvider
{
    using PX.Data;

    [System.Serializable()]
	public class BoxScreenGroupingFields : IBqlTable
    {
		#region ScreenID
		public abstract class screenID : IBqlField
        {
		}
		protected string _ScreenID;
		[PXDBString(8, IsKey = true)]
		[PXUIField(DisplayName = "Screen")]
        [PXDefault(typeof(BoxScreenConfiguration.screenID))]
        [PXParent(typeof(Select<BoxScreenConfiguration,
            Where<BoxScreenConfiguration.screenID, Equal<Current<BoxScreenGroupingFields.screenID>>>>))]
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
		#region LineNbr
		public abstract class lineNbr : IBqlField
        {
		}
		protected int? _LineNbr;
		[PXDBInt(IsKey = true)]
		[PXDefault()]
		[PXUIField(DisplayName = "LineNbr")]
        [PXLineNbr(typeof(BoxScreenConfiguration.lineCntr))]
        public virtual int? LineNbr
		{
			get
			{
				return _LineNbr;
			}
			set
			{
                _LineNbr = value;
			}
		}
		#endregion
		#region FieldName
		public abstract class fieldName : IBqlField
        {
		}
		protected string _FieldName;
		[PXDBString(128)]
		[PXDefault("")]
		[PXUIField(DisplayName = "Field Name")]
        [PXStringList(new string[] { "screenNotSelected" }, new string[] { "Please select a screen first..." })]
        public virtual string FieldName
		{
			get
			{
				return _FieldName;
			}
			set
			{
                _FieldName = value;
			}
		}
		#endregion
	}
}
