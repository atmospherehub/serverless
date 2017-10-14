/***************************************************************
[Faces]
***************************************************************/
CREATE TABLE [dbo].[Faces](
	[Id] [uniqueidentifier] NOT NULL,
	[Time] [datetimeoffset](7) NOT NULL,
	[Image] [varchar](40) NOT NULL,
	[Rectangle] [varchar](max) NOT NULL,
	[CognitiveAnger] [real] NOT NULL,
	[CognitiveContempt] [real] NOT NULL,
	[CognitiveDisgust] [real] NOT NULL,
	[CognitiveFear] [real] NOT NULL,
	[CognitiveHappiness] [real] NOT NULL,
	[CognitiveNeutral] [real] NOT NULL,
	[CognitiveSadness] [real] NOT NULL,
	[CognitiveSurprise] [real] NOT NULL,
	[UserId] [varchar](10) NULL,
 CONSTRAINT [PK_Faces] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
)
GO
CREATE NONCLUSTERED INDEX [IX_CognitiveAnger] ON [dbo].[Faces]
(
	[CognitiveAnger] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
GO
CREATE NONCLUSTERED INDEX [IX_CognitiveContempt] ON [dbo].[Faces]
(
	[CognitiveContempt] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
GO
CREATE NONCLUSTERED INDEX [IX_CognitiveDisgust] ON [dbo].[Faces]
(
	[CognitiveDisgust] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
GO
CREATE NONCLUSTERED INDEX [IX_CognitiveFear] ON [dbo].[Faces]
(
	[CognitiveFear] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
GO
CREATE NONCLUSTERED INDEX [IX_CognitiveHappiness] ON [dbo].[Faces]
(
	[CognitiveHappiness] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
GO
CREATE NONCLUSTERED INDEX [IX_CognitiveNeutral] ON [dbo].[Faces]
(
	[CognitiveNeutral] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
GO
CREATE NONCLUSTERED INDEX [IX_CognitiveSadness] ON [dbo].[Faces]
(
	[CognitiveSadness] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
GO
CREATE NONCLUSTERED INDEX [IX_CognitiveSurprise] ON [dbo].[Faces]
(
	[CognitiveSurprise] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
GO
CREATE NONCLUSTERED INDEX [IX_Image] ON [dbo].[Faces]
(
	[Image] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
GO
CREATE NONCLUSTERED INDEX [IX_Time] ON [dbo].[Faces]
(
	[Time] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
GO
CREATE NONCLUSTERED INDEX [IX_UserId] ON [dbo].[Faces]
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
GO

/***************************************************************
[FaceTags]
***************************************************************/
CREATE TABLE [dbo].[FaceTags](
	[FaceId] [uniqueidentifier] NOT NULL,
	[UserId] [varchar](10) NOT NULL,
	[TaggedByUserId] [varchar](10) NOT NULL,
	[TaggedByName] [varchar](256) NOT NULL,
	[Time] [datetimeoffset](7) NOT NULL,
 CONSTRAINT [pk_UserVote] PRIMARY KEY CLUSTERED 
(
	[FaceId] ASC,
	[TaggedByUserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
)
GO

/***************************************************************
[ReportsSubscribers]
***************************************************************/
CREATE TABLE [dbo].[ReportsSubscribers](
	[Email] [varchar](256) NOT NULL,
	[Name] [nvarchar](256) NULL,
	[IsDisabled] [bit] NOT NULL,
 CONSTRAINT [PK_ReportsSubscribers] PRIMARY KEY CLUSTERED 
(
	[Email] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
)
GO

/***************************************************************
[UsersMap]
***************************************************************/
CREATE TABLE [dbo].[UsersMap](
	[UserId] [varchar](10) NOT NULL,
	[CognitiveUid] [varchar](36) NOT NULL,
	[FirstName] [nvarchar](256) NULL,
	[LastName] [nvarchar](256) NULL,
	[Email] [nvarchar](256) NULL,
 CONSTRAINT [PK_UsersMap] PRIMARY KEY CLUSTERED 
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
)
GO
CREATE UNIQUE NONCLUSTERED INDEX [IDX_CognitiveUid] ON [dbo].[UsersMap]
(
	[CognitiveUid] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
GO
CREATE NONCLUSTERED INDEX [IX_Email] ON [dbo].[UsersMap]
(
	[Email] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
GO
