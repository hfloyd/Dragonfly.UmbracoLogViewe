namespace Dragonfly.UmbracoLogViewer.Models
{
    using Umbraco.Core.Persistence;
    using Umbraco.Core.Persistence.DatabaseAnnotations;

    [TableName("LogViewerTagRules")]
    [PrimaryKey("Id", autoIncrement = true)]
    public class TagRule
    {
        internal static string TableName = "LogViewerTagRules";

        internal static string CreateSql = @"
        CREATE TABLE [LogViewerTagRules](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[RuleType] [varchar](50) NOT NULL,
	[TextToMatch] [varchar](2000) NOT NULL,
	[ExactMatch] [bit] NOT NULL,
	[Tag] [varchar](50) NOT NULL,
	[TagCategory] [varchar](50) NOT NULL
) ON [PRIMARY]";


        [PrimaryKeyColumn(AutoIncrement = true)]
        public int Id { get; set; }

        public string RuleType { get; set; }

        public string TextToMatch { get; set; }
        public bool ExactMatch { get; set; }
        public string Tag { get; set; }
        public string TagCategory { get; set; }
    }
}
