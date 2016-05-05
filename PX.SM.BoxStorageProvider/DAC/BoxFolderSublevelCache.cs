﻿namespace PX.SM.BoxStorageProvider
{
	using System;
	using PX.Data;
	
	[System.SerializableAttribute()]
	public class BoxFolderSublevelCache : PX.Data.IBqlTable
	{
		#region ScreenID
		public abstract class screenID : PX.Data.IBqlField
		{
		}
		protected string _ScreenID;
		[PXDBString(8, IsKey = true)]
		[PXDefault()]
		[PXUIField(DisplayName = "ScreenID")]
		public virtual string ScreenID
		{
			get
			{
				return this._ScreenID;
			}
			set
			{
				this._ScreenID = value;
			}
		}
		#endregion
		#region Grouping
		public abstract class grouping : PX.Data.IBqlField
		{
		}
		protected string _Grouping;
		[PXDBString(256, IsUnicode = true, IsKey = true)]
		[PXDefault()]
		[PXUIField(DisplayName = "Grouping")]
		public virtual string Grouping
		{
			get
			{
				return this._Grouping;
			}
			set
			{
				this._Grouping = value;
			}
		}
		#endregion
		#region FolderID
		public abstract class folderID : PX.Data.IBqlField
		{
		}
		protected string _FolderID;
		[PXDBString(256, IsUnicode = true)]
		[PXDefault("")]
		[PXUIField(DisplayName = "FolderID")]
		public virtual string FolderID
		{
			get
			{
				return this._FolderID;
			}
			set
			{
				this._FolderID = value;
			}
		}
		#endregion
	}
}
