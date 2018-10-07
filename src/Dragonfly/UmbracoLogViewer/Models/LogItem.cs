namespace Dragonfly.UmbracoLogViewer.Models
{
    using System;
    using System.Collections.Generic;
    using Umbraco.Core.Persistence;
    using Umbraco.Core.Persistence.DatabaseAnnotations;

    [TableName("LogViewerLog")]
    [PrimaryKey("Id", autoIncrement = true)]
    public class LogItem
    {
        internal static string TableName = "LogViewerLog";

        internal static string CreateSql = @"
        CREATE TABLE [LogViewerLog](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Date] [datetime] NOT NULL,
	[Thread] [varchar](255) NOT NULL,
	[Level] [varchar](50) NOT NULL,
	[Logger] [varchar](255) NOT NULL,
	[Message] [varchar](4000) NOT NULL,
	[Exception] [varchar](2000) NULL,
	[Host] [varchar](2000) NULL,
	[UrlPath] [varchar](2000) NULL,
	[ProcessUserName] [varchar](255) NULL,
	[Properties] [varchar](4000) NULL,
	[Resolved] [bit] NULL CONSTRAINT [DF_Log_Resolved]  DEFAULT ((0))
) ON [PRIMARY]";

        [PrimaryKeyColumn(AutoIncrement = true)]
        public int Id { get; set; }

        public DateTime Date { get; set; }
        public string Thread { get; set; }
        public string Level { get; set; }
        public string Logger { get; set; }
        public string Message { get; set; }

        [NullSetting(NullSetting = NullSettings.Null)]
        public string Exception { get; set; }

        [NullSetting(NullSetting = NullSettings.Null)]
        public string Host { get; set; }

        [NullSetting(NullSetting = NullSettings.Null)]
        public string UrlPath { get; set; }

        [NullSetting(NullSetting = NullSettings.Null)]
        public string ProcessUserName { get; set; }

        [NullSetting(NullSetting = NullSettings.Null)]
        public string Properties { get; set; }

        public bool Resolved { get; set; }

        [Ignore]
        public bool HasValidUrlPath
        {
            get
            {
                if (this.UrlPath.StartsWith("/"))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        //There might be a better way to handle this, but for now let's see how performance is
        [Ignore]
        public bool TagDataUpdated { get; set; }

        [Ignore]
        public IEnumerable<string> SiteTags { get; set; }

        [Ignore]
        public IEnumerable<string> LevelTags { get; set; }

    }
}

