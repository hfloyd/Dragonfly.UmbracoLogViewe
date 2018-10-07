namespace Dragonfly.UmbracoLogViewer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Dragonfly.UmbracoLogViewer.Models;
    using Umbraco.Core;
    using Umbraco.Core.Persistence;

    public class LogViewerService
    {

        #region DB Access
        
        DatabaseContext dc = ApplicationContext.Current.DatabaseContext;
        //private bool useUmbracoDb = LogViewerInternalHelper.UseUmbracoDb();
        private Database _logDb = LogViewerInternalHelper.GetLogDb();
        
        #endregion


        #region LogItem DB Stuff

        public LogItem GetLogItem(int Id, bool UpdateTagData=true)
        {
            var item = _logDb.SingleOrDefault<LogItem>(Id);

            if (UpdateTagData)
            {
                item = UpdateLogItemTagData(item);
            }

            return item;
        }

        public IEnumerable<LogItem> GetAllLogItems(bool IncludeResolved = true,bool UpdateTagData=true)
        {
            Sql sql;

            if (IncludeResolved)
            {
                sql = new Sql()
                    .Select("*")
                    .From<LogItem>(dc.SqlSyntax)
                    .OrderByDescending("Date");
            }
            else
            {
                sql = new Sql()
                    .Select("*")
                    .From<LogItem>(dc.SqlSyntax)
                    .Where<LogItem>(n=> n.Resolved == false, dc.SqlSyntax)
                    .OrderByDescending("Date");
            }
            
            var items = _logDb.Fetch<LogItem>(sql);

            if (UpdateTagData)
            {
                items = UpdateAllLogItemsTagData(items);
            }

            return items;
        }

        

        public IEnumerable<LogItem> GetAllLogItems(DateTime SinceDate, bool IncludeResolved = true, bool UpdateTagData=true)
        {
            Sql sql;

            if (IncludeResolved)
            {
                sql = new Sql()
                    .Select("*")
                    .From<LogItem>(dc.SqlSyntax)
                    .Where<LogItem>(n => n.Date >= SinceDate, dc.SqlSyntax)
                    .OrderByDescending("Date");
            }
            else
            {
                sql = new Sql()
                    .Select("*")
                    .From<LogItem>(dc.SqlSyntax)
                    .Where<LogItem>(n => n.Resolved == false && n.Date >= SinceDate, dc.SqlSyntax)
                    .OrderByDescending("Date");
            }

            var items = _logDb.Fetch<LogItem>(sql);

            return items;
        }

        //public static LogItem Create(string name, string description)
        //{
        //    var competition = new Data.Poco.Competition
        //    {
        //        Name = name,
        //        Description = description,
        //        CreatedOn = DateTime.UtcNow
        //    };

        //    _logDb.Insert(competition);

        //    return competition;
        //}

        #endregion

        #region TagRule DB Stuff

        public TagRule GetTagRule(int Id)
        {
            var item = _logDb.SingleOrDefault<TagRule>(Id);

            return item;
        }

        public IEnumerable<TagRule> GetAllTagRules()
        {
            var sql = new Sql()
                .Select("*")
                .From<TagRule>(dc.SqlSyntax)
                .OrderBy("RuleType");

            var items = _logDb.Fetch<TagRule>(sql);

            return items;
        }

        public IEnumerable<TagRule> GetAllTagRulesByType(string RuleType)
        {
            var sql = new Sql()
                .Select("*")
                .From<TagRule>(dc.SqlSyntax)
                .Where<TagRule>(n => n.RuleType == RuleType, dc.SqlSyntax);

            var items = _logDb.Fetch<TagRule>(sql);

            return items;
        }

        public IEnumerable<TagRule> GetAllTagRulesByTagCategory(string TagCategory)
        {
            var sql = new Sql()
                .Select("*")
                .From<TagRule>(dc.SqlSyntax)
                .Where<TagRule>(n => n.TagCategory == TagCategory, dc.SqlSyntax);

            var items = _logDb.Fetch<TagRule>(sql);

            return items;
        }

        #endregion

        #region Business Logic

        private List<LogItem> UpdateAllLogItemsTagData(List<LogItem> Items)
        {
            var updatedItems = new List<LogItem>();

            foreach (var logItem in Items)
            {
                updatedItems.Add(UpdateLogItemTagData(logItem));
            }

            return updatedItems;
        }

        private LogItem UpdateLogItemTagData(LogItem Item)
        {
            Item.TagDataUpdated = true;
            Item.SiteTags = LogItemTags(Item, "Site");
            Item.LevelTags = LogItemTags(Item, "Level");

            return Item;
        }

        public IEnumerable<string> LogItemTags(LogItem Log, string TagCategory = "")
        {
            var tagsList = new List<string>();

            IEnumerable<TagRule> rulesToProcess = new List<TagRule>();

            if (TagCategory != "")
            {
                rulesToProcess = GetAllTagRulesByTagCategory(TagCategory);
            }
            else
            {
                rulesToProcess = GetAllTagRules();
            }

            if (rulesToProcess.Any())
            {
                foreach (var rule in rulesToProcess)
                {
                    var tag = ProcessTagRule(rule, Log);
                    if (tag != "")
                    { tagsList.Add(tag); }
                }
            }

            return tagsList;
        }

        private string ProcessTagRule(TagRule Rule, LogItem Log)
        {
            switch (Rule.RuleType)
            {
                case "Host":
                    if (Rule.ExactMatch)
                    {
                        if (Log.Host == Rule.TextToMatch)
                        {
                            return Rule.Tag;
                        }
                        return "";
                    }
                    else
                    {
                        if (Log.Host.Contains(Rule.TextToMatch))
                        {
                            return Rule.Tag;
                        }
                        return "";
                    }
                    
                case "Logger":
                    if (Rule.ExactMatch)
                    {
                        if (Log.Logger == Rule.TextToMatch)
                        {
                            return Rule.Tag;
                        }
                        return "";
                    }
                    else
                    {
                        if (Log.Logger.Contains(Rule.TextToMatch))
                        {
                            return Rule.Tag;
                        }
                        return "";
                    }

                default:
                    return $"Unknown RuleType {Rule.RuleType}";
            }

        }

        #endregion
    }
}
