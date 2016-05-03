CREATE TABLE [dbo].[BoxFileCache](
	[CompanyID] [int] NOT NULL,
	[BlobHandler] [uniqueidentifier] NOT NULL,
	[FileID] [nvarchar](256) NOT NULL,
	[ParentFolderID] [nvarchar](256) NULL,
 CONSTRAINT [BoxFileCache_PK] PRIMARY KEY CLUSTERED 
(
	[CompanyID] ASC,
	[BlobHandler] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

ALTER TABLE [dbo].[BoxFileCache] ADD  DEFAULT ((0)) FOR [CompanyID]
GO

CREATE TABLE [dbo].[BoxFolderCache](
	[CompanyID] [int] NOT NULL,
	[FolderID] [nvarchar](256) NOT NULL,
	[ActivityFolderID] [nvarchar](256) NULL,
	[LastModifiedDateTime] [datetime] NULL,
	[RefNoteID] [uniqueidentifier] NULL,
	[ParentFolderID] [nvarchar](256) NULL,
	[ScreenID] [varchar](8) NULL
 CONSTRAINT [BoxFolderCache_PK] PRIMARY KEY CLUSTERED 
(
	[CompanyID] ASC,
	[FolderID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

ALTER TABLE [dbo].[BoxFolderCache] ADD  DEFAULT ((0)) FOR [CompanyID]
GO

CREATE NONCLUSTERED INDEX [BoxFolderCache_ScreenID_index] ON [dbo].[BoxFolderCache]
(
	[CompanyID] ASC,
	[ScreenID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
GO

CREATE NONCLUSTERED INDEX [BoxFolderCache_RefNoteID_index] ON [dbo].[BoxFolderCache]
(
	[CompanyID] ASC,
	[RefNoteID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
GO



CREATE TABLE [dbo].[BoxUserTokens](
	[CompanyID] [int] NOT NULL,
	[UserID] [uniqueidentifier] NOT NULL,
	[AccessToken] [nvarchar](255) NULL,
	[BoxUserID] [nvarchar](255) NULL,
	[BoxEmailAddress] [nvarchar](255) NULL,
	[SiteAdministrator] [bit] NULL,
	[RefreshToken] [nvarchar](255) NULL,
	[RefreshTokenDate] [datetime] NULL,
 CONSTRAINT [BoxUserTokens_PK] PRIMARY KEY CLUSTERED 
(
	[CompanyID] ASC,
	[UserID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

ALTER TABLE [dbo].[BoxUserTokens] ADD  DEFAULT ((0)) FOR [CompanyID]
GO

CREATE TABLE [dbo].[BoxScreenConfiguration](
	[CompanyID] [int] NOT NULL,
	[ScreenID] [varchar](8) NOT NULL,
	[LineCntr] [int] NOT NULL,
 CONSTRAINT [BoxScreenConfiguration_PK] PRIMARY KEY CLUSTERED 
(
	[CompanyID] ASC,
	[ScreenID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

CREATE TABLE [dbo].[BoxScreenGroupingFields](
	[CompanyID] [int] NOT NULL,
	[ScreenID] [varchar](8) NOT NULL,
	[LineNbr] [int] NOT NULL,
	[FieldName] [varchar](128) NOT NULL
 CONSTRAINT [BoxScreenGroupingFields_PK] PRIMARY KEY CLUSTERED 
(
	[CompanyID] ASC,
	[ScreenID] ASC,
	[LineNbr] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

