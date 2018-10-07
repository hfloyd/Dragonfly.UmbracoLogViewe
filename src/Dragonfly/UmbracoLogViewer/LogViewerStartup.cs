namespace Dragonfly.UmbracoLogViewer
{
    using System;
    using Dragonfly.UmbracoLogViewer.Models;
    using Umbraco.Core;
    using Umbraco.Core.Logging;
    using Umbraco.Core.Persistence;

    public class LogViewerStartup : ApplicationEventHandler
    {
        protected override void ApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            if (LogViewerInternalHelper.AllConfigsPresent())
            {
                //Umbraco DB
                if (LogViewerInternalHelper.UseUmbracoDb())
                {
                    CreateNewTables();
                }
                else
                {
                    //External DB
                    var logDbIsValid = LogViewerInternalHelper.DbConnectionIsValid("LogDbDSN");
                    if (logDbIsValid)
                    {
                        CreateNewTables();
                    }
                    else
                    {
                        var msgDb =
                            $"Dragonfly.UmbracoLogViewer is unable to connect to the 'LogDbDSN' Database. Please check your web.config and your network connectivity.";
                        LogHelper.Error<LogViewerStartup>(msgDb, null);
                    }
                }
            }
        }


        private void CreateNewTables()
        {
            try
            {
                //DatabaseContext ctx = ApplicationContext.Current.DatabaseContext;

                using (var db = LogViewerInternalHelper.GetLogDb())
                {
                    db.OpenSharedConnection();
                    //DatabaseSchemaHelper dbSchema = new DatabaseSchemaHelper(db,
                    //    ApplicationContext.Current.ProfilingLogger.Logger, ctx.SqlSyntax);

                    //if (dbSchema.TableExist("LogViewerLog")) dbSchema.DropTable<LogItem>();
                    //if (dbSchema.TableExist("LogViewerTagRules")) dbSchema.DropTable<TagRule>();

                    var msgDb = $"LogViewer CreateNewTables() running for database '{db.Connection.Database}'.";
                    LogHelper.Debug<LogViewerStartup>(msgDb);

                    CreateTable(db, LogItem.TableName, LogItem.CreateSql);

                    CreateTable(db, TagRule.TableName, TagRule.CreateSql);

                }
            }
            catch (Exception e)
            {
                var msg = $"Error occurred during LogViewer tables creation for Database. (You specified Use Umbraco DB: {LogViewerInternalHelper.UseUmbracoDb()})";
                LogHelper.Error<LogViewerStartup>(msg, e);
            }
        }

        private void CreateTable(Database Db, string TableName, string CreationSql)
        {
            try
            {
                var tblExists = Db.TableExist(TableName); //.Replace("dbo.","")
                LogHelper.Debug<LogViewerStartup>($"====Table '{TableName}' Exists: {tblExists}");

                if (!tblExists)
                {
                    Db.Execute(CreationSql);
                    var msg =
                        $"New LogViewer database table '{TableName}' created in Database '{Db.Connection.Database}'.";
                    LogHelper.Info<LogViewerStartup>(msg);
                }
                else
                {
                    var msg =
                        $"LogViewer table '{TableName}' already exists in Database '{Db.Connection.Database}'.";
                    LogHelper.Debug<LogViewerStartup>(msg);
                }
                LogHelper.Debug<LogViewerStartup>($"====/Table '{TableName}'");
            }
            catch (Exception e)
            {
                var msg = $"Error occurred during creation of table '{TableName}' in Database '{Db.Connection.Database}'.";
                LogHelper.Error<LogViewerStartup>(msg, e);
            }
        }
    }

}
