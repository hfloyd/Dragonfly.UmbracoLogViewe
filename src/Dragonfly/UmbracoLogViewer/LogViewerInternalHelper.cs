using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dragonfly.UmbracoLogViewer
{
    using System.Configuration;
    using System.Data;
    using System.Data.SqlClient;
    using System.Web.Configuration;
    using Umbraco.Core;
    using Umbraco.Core.Logging;
    using Umbraco.Core.Persistence;

    internal static class LogViewerInternalHelper
    {
        internal static bool AllConfigsPresent()
        {
            var configUseUmbracoDb = WebConfigurationManager.AppSettings["UseUmbracoDb"];
            var configLogDbDSN = WebConfigurationManager.AppSettings["LogDbDSN"];

            if (configUseUmbracoDb == null)
            {
                var msg = "The \"UseUmbracoDb\" AppSetting is missing from the web.config. Please review your setup.";
                LogHelper.Error<LogViewerStartup>(msg, null);
                return false;
            }
            else if (configLogDbDSN == null)
            {
                var msg = "The \"LogDbDSN\" AppSetting is missing from the web.config. Please review your setup.";
                LogHelper.Error<LogViewerStartup>(msg, null);
                return false;
            }
            else
            {
                //All configs present
                return true;
            }
        }

        internal static bool UseUmbracoDb()
        {
            var configUseUmbracoDb = WebConfigurationManager.AppSettings["UseUmbracoDb"];
            var useUmbracoDb = configUseUmbracoDb.ToLower() == "true" ? true : false;
            return useUmbracoDb;
        }

        internal static Database GetLogDb()
        {
            try
            {
                if (UseUmbracoDb())
                {
                    return ApplicationContext.Current.DatabaseContext.Database;
                }
                else
                {
                    var external = new Database("LogDbDSN");
                    return external;
                }
            }
            catch (Exception e)
            {
                var msg = $"Error occurred with Dragonfly.UmbracoLogViewer GetLogDb() (You specified Use Umbraco DB: {UseUmbracoDb()})";
                LogHelper.Error<Database>(msg, e);
                return null;
            }
        }

        internal static bool DbConnectionIsValid(string ConnectionStringName)
        {
            if (ConfigurationManager.ConnectionStrings[ConnectionStringName] == null)
            {
                var msg =
                    $"Error occurred with Dragonfly.UmbracoLogViewer DbConnectionIsValid(). There is no connectionstring matching name '{ConnectionStringName}' in the Web.config.";
                LogHelper.Warn<Database>(msg);
                return false;
            }
            else
            {
                var connectionString = ConfigurationManager.ConnectionStrings[ConnectionStringName].ConnectionString;

                if (connectionString == "")
                {
                    var msg =
                        $"Error occurred with Dragonfly.UmbracoLogViewer DbConnectionIsValid(). The value for connectionstring '{ConnectionStringName}' in the Web.config is blank.";
                    LogHelper.Warn<Database>(msg);
                    return false;
                }
                else
                {
                    try
                    {
                        using (var connection = new SqlConnection(connectionString))
                        {
                            connection.Open();

                            if ((connection.State & ConnectionState.Open) > 0)
                            {
                                var msg =
                                    $"Dragonfly.UmbracoLogViewer DbConnectionIsValid() successfully connected to '{ConnectionStringName}'";
                                LogHelper.Debug<Database>(msg);
                                return true;
                            }
                            else
                            {
                                var msg =
                                    $"Dragonfly.UmbracoLogViewer DbConnectionIsValid() failed when trying to connect to '{ConnectionStringName}' with string '{connectionString}' - connection.State = {connection.State}";
                                LogHelper.Warn<Database>(msg);
                                return false;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        var msg =
                            $"Error occurred with Dragonfly.UmbracoLogViewer DbConnectionIsValid() when trying to connect to '{ConnectionStringName}':";
                        LogHelper.Error<Database>(msg, e);
                        return false;
                    }

                }
            }
        }
    }
}
