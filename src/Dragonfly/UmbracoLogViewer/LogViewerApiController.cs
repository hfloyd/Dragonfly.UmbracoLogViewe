namespace Dragonfly.UmbracoLogViewer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Web;
    using System.Web.Mvc;
    using Dragonfly.NetModels;
    using Dragonfly.UmbracoModels;
    using Newtonsoft.Json;
    using Umbraco.Core.Models;
    using Umbraco.Web;
    using Umbraco.Web.WebApi;

    // [IsBackOffice]
    // /Umbraco/backoffice/Api/LogViewerApi <-- UmbracoAuthorizedApiController

    [IsBackOffice]
    public class LogViewerApiController : UmbracoAuthorizedApiController
    {

        #region LogViewer

        /// /Umbraco/backoffice/Api/LogViewerApi/LogViewer?ShowResolved=true&FilterSite="xxx"&FilterLevel="xxx"
        [System.Web.Http.AcceptVerbs("GET")]
        public HttpResponseMessage LogViewer(bool ShowResolved = false, string FilterSite = "", string FilterLevel = "")
        {
            var returnSB = new StringBuilder();
            var fancyFormat = true;
            var customStyles = "";
            var customScripts = "";
            //TODO: Update to use partial views
            customStyles = @"
                        .container{
                            margin-left: 1em;
                            margin-right: 1em;
                        }";

            customScripts = @"
                $(document).ready(function() {
                    $('#umbracodata').DataTable( {
                        'lengthMenu': [[25, 50, 100, -1], [25, 50, 100, 'All']]
                    } );
                });";

            var extraHeaders = "";
            if (fancyFormat)
            {
                extraHeaders = @"
                        <th>#</th>
                        <th>Actions</th> ";
            }

            //GET LOG ENTRIES TO DISPLAY
            var lvs = new LogViewerService();
            var allLogs = lvs.GetAllLogItems(ShowResolved).ToList();
            var logsFiltered = allLogs;
            bool isFiltered = false;
            var filtersList = new List<string>();

            if (FilterSite != "")
            {
                isFiltered = true;
                logsFiltered = logsFiltered.Where(n => n.Host == FilterSite || n.SiteTags.Contains(FilterSite)).ToList();
                filtersList.Add($"Site '{FilterSite}'");
            }

            if (FilterLevel != "")
            {
                isFiltered = true;
                logsFiltered = logsFiltered.Where(n => n.Level == FilterLevel || n.LevelTags.Contains(FilterLevel)).ToList();
                filtersList.Add($"Level '{FilterLevel}'");
            }


            //BUILD HTML
            returnSB.AppendLine(HtmlStart(customStyles));
            returnSB.AppendLine($"<h1>Log Viewer</h1>");

            //Show/Hide Resolved Items
            var showResolvedClass = ShowResolved ? "active" : "";
            var hideResolvedClass = !ShowResolved ? "active" : "";
            var showResolvedUrl = Dragonfly.NetHelpers.Url.AppendQueryStringToUrl(Request.RequestUri, "ShowResolved", "true");
            var hideResolvedUrl = Dragonfly.NetHelpers.Url.AppendQueryStringToUrl(Request.RequestUri, "ShowResolved", "false");
            var resolvedButtons = $@"
                <div class=""btn-group"" data-toggle=""buttons"">
                    <a class=""btn btn-default btn-sm {hideResolvedClass}"" href=""{hideResolvedUrl}"">Hide Resolved</a>
                    <a class=""btn btn-default btn-sm {showResolvedClass}"" href=""{showResolvedUrl}"">Show All</a>
                </div>
                ";
            returnSB.AppendLine(resolvedButtons);
            returnSB.AppendLine($"<p>Log Items Returned: {logsFiltered.Count()} (of {allLogs.Count} total visible log items)</p>");


            if (isFiltered)
            {
                var filtersString = Dragonfly.NetHelpers.Strings.JoinAsText(", ", " and ", filtersList);
                returnSB.AppendLine(string.Format("<p>Displaying {0} log items with {1}.</p>", logsFiltered.Count(), filtersString));
                returnSB.AppendLine($"<p><b><a class=\"btn btn-success\" href=\"LogViewer?ShowResolved={ShowResolved}\">Remove Filters and Show All</a></b></p>");
            }

            var tableDataHeaders = $@"
                        <th><small>Resolved<small></th>                        
                        <th>Site</th>
                        <th>Date/Time</th>
                        <th>Level</th>
                        <th>Type</th>
                        <th>Message</th>
                        <th>Page</th>
                        ";

            var tableStart = $@"
                <table  id=""umbracodata"" class=""table table-striped table-bordered table-hover table-sm"" cellspacing=""0"" style=""width:100%""> 
                    <thead>
                    <tr>
                        {extraHeaders}
                        {tableDataHeaders}
                    </tr>
                    </thead>
                    <tbody>
                    ";

            var tableEnd = @"</tbody></table>";

            var tableData = new StringBuilder();
            var counter = 0;

            //Special Classes for Tags
            //BOOTSTRAP COLOR OPTIONS: muted primary success info warning danger
            var levelTagClasses = new Dictionary<string, string>()
            {
                { "INFO", "info"},
                { "WARN", "warning"},
                { "ERROR", "danger"},
                { "Security", "danger"}
            };

            //LOOP 
            foreach (var item in logsFiltered)
            {
                var pageUrl = item.HasValidUrlPath ? $"http://{item.Host}{item.UrlPath}" : "";

                //Render Table
                counter++;

                tableData.AppendLine("<tr>");

                if (fancyFormat)
                {
                    // #
                    tableData.AppendLine(TdStringData(counter, fancyFormat));

                    //Actions
                    tableData.AppendLine(TdLogActions(item.Id, pageUrl));
                }

                //Resolved
                var resolvedTrue = @"<div>
                            <span class=""glyphicon glyphicon-ok-circle"" aria-hidden=""true"" style=""font-size:30px;color:#5cb85c;""></span>
                            <span class=""sr-only"">Resolved</span>
                        </div>";
                var resolvedFalse = @"<div class=""text-muted"">
                          <span class=""glyphicon glyphicon-ban-circle"" aria-hidden=""true"" style=""font-size:30px;""></span>
                          <span class=""sr-only"">Not Yet Resolved</span>
                        </div>";
                tableData.AppendLine(TdBoolData(item.Resolved, fancyFormat, resolvedTrue, resolvedFalse));

                //Site
                var siteTags = new List<string>();
                siteTags.Add(item.Host);
                siteTags.AddRange(item.SiteTags);
                tableData.AppendLine(TdTagData(siteTags, fancyFormat, "FilterSite", null));

                //Date 
                tableData.AppendLine(TdDateData(item.Date, fancyFormat));

                //Level
                var levelTags = new List<string>();
                levelTags.Add(item.Level);
                levelTags.AddRange(item.LevelTags);
                tableData.AppendLine(TdTagData(levelTags, fancyFormat, "FilterLevel", levelTagClasses));

                //Type (Logger)
                tableData.AppendLine(TdStringData(item.Logger, fancyFormat));

                //Message
                tableData.AppendLine(TdStringData(item.Message, fancyFormat));

                //Page
                tableData.AppendLine(TdStringData(item.UrlPath, fancyFormat));

                tableData.AppendLine("</tr>");
            }

            returnSB.AppendLine(tableStart);
            returnSB.Append(tableData);
            returnSB.AppendLine(tableEnd);

            if (fancyFormat)
            {
                returnSB.AppendLine(HtmlEnd(customScripts));
            }

            return new HttpResponseMessage()
            {
                Content = new StringContent(
                    returnSB.ToString(),
                    Encoding.UTF8,
                    "text/html"
                )
            };
        }

        /// /Umbraco/backoffice/Api/LogViewerApi/LogDetailsViewer?LogId=1234
        [System.Web.Http.AcceptVerbs("GET")]
        public HttpResponseMessage LogDetailsViewer(int LogId = 0)
        {
            var returnSB = new StringBuilder();
            var fancyFormat = true;
            var customStyles = "";
            var customScripts = "";

            //customStyles = @"
            //            .container{
            //                margin-left: 1em;
            //                margin-right: 1em;
            //            }";

            //customScripts = @"
            //    $(document).ready(function() {
            //        $('#umbracodata').DataTable( {
            //            'lengthMenu': [[25, 50, 100, -1], [25, 50, 100, 'All']]
            //        } );
            //    });";


            //GET LOG ENTRIES TO DISPLAY
            var lvs = new LogViewerService();
            var logItem = lvs.GetLogItem(LogId);

            //BUILD HTML
            returnSB.AppendLine(HtmlStart(customStyles));
            returnSB.AppendLine($"<h1>Log Details Viewer</h1>");

            //RENDER DATA 
            if (logItem != null)
            {
                var pvPath = "~/Views/Partials/LogViewer/LogItemDetails.cshtml";

                var logItemHtml = Dragonfly.Umbraco7Helpers.ApiControllerHtmlHelper.GetPartialViewHtml(this.ControllerContext, pvPath, new ViewDataDictionary(logItem), HttpContext.Current);
                returnSB.AppendLine(logItemHtml);
            }

            if (fancyFormat)
            {
                returnSB.AppendLine(HtmlEnd(customScripts));
            }

            return new HttpResponseMessage()
            {
                Content = new StringContent(
                    returnSB.ToString(),
                    Encoding.UTF8,
                    "text/html"
                )
            };
        }

        /// /Umbraco/backoffice/Api/LogViewerApi/UpdateLogItemResolution?LogId=xxx&Resolved=true
        [System.Web.Http.AcceptVerbs("GET")]
        public HttpResponseMessage UpdateLogItemResolution(bool Resolved, int LogId = 0)
        {
            var returnSB = new StringBuilder();

            var testData = new StatusMessage(false, "Not yet implemented...");
            string json = JsonConvert.SerializeObject(testData);

            returnSB.AppendLine(json);

            return new HttpResponseMessage()
            {
                Content = new StringContent(
                    returnSB.ToString(),
                    Encoding.UTF8,
                    "application/json"
                )
            };
        }


        #endregion

        #region Fancy Formatting & Tables Data

        private string HtmlStart(string CustomStyles = "")
        {
            var pageStart = $@"<!DOCTYPE html>
                <html>
                <head>
                    <link href=""https://maxcdn.bootstrapcdn.com/bootstrap/3.3.7/css/bootstrap.min.css"" rel=""stylesheet""/>
                    <link href=""https://cdnjs.cloudflare.com/ajax/libs/datatables/1.10.12/css/dataTables.bootstrap.min.css"" rel=""stylesheet""/>

                    <style>
                        {CustomStyles}
                    </style>
                    <!--<script src=""/scripts/snippet-javascript-console.min.js?v=1""></script>-->
                </head>
                <body>                
                    <div class=""container"">
            
               ";

            return pageStart;
        }

        private string HtmlEnd(string CustomScripts = "")
        {
            var scriptDefault = @" 
                      $(document).ready(function() {
                      $('#umbracodata').DataTable();
                    });
               ";

            var scripts = CustomScripts != "" ? CustomScripts : scriptDefault;

            var pageStart = $@"
            </div>
                <script src=""https://cdnjs.cloudflare.com/ajax/libs/jquery/3.1.1/jquery.min.js""></script>
                <script src=""https://cdnjs.cloudflare.com/ajax/libs/datatables/1.10.12/js/jquery.dataTables.min.js""></script>
                <script src=""https://cdnjs.cloudflare.com/ajax/libs/datatables/1.10.12/js/dataTables.bootstrap.min.js""></script>
                <script type=""text/javascript"">                
                    {scripts}
                </script>
            </body>
            </html>
               ";

            return pageStart;
        }

        private string TdLogActions(int LogId, string PageUrl)
        {
            var tableData = new StringBuilder();

            var detailsUrl = $"LogDetailsViewer?LogId={LogId}";
            var detailsButton = $@"<a class=""btn btn-default btn-sm"" href=""{detailsUrl}"" target=""_blank"" aria-label=""Details"">
                <span class=""glyphicon glyphicon-info-sign"" aria-hidden=""true"" ></span>
                </a>";

            var viewButton = $@"<a class=""btn btn-default btn-sm"" href=""{PageUrl}"" target=""_blank"" aria-label=""View Page"">
                <span class=""glyphicon glyphicon-eye-open"" aria-hidden=""true"" ></span>
                </a>";

            tableData.AppendLine("<td>");
            tableData.AppendLine(detailsButton);

            if (PageUrl != "")
            {
                tableData.AppendLine(viewButton);
            }

            tableData.AppendLine("</td>");

            return tableData.ToString();
        }

        private string TdActions(int NodeId, string NodeUrl)
        {
            var tableData = new StringBuilder();

            tableData.AppendLine(string.Format("<td>"));
            tableData.AppendLine(string.Format(
                "<a href=\"/umbraco#/content/content/edit/{0}\" target=\"_blank\">Edit</a>", NodeId));
            if (NodeUrl != "")
            {
                tableData.AppendLine(string.Format(" | <a href=\"{0}\" target=\"_blank\">View</a> ",
                    NodeUrl));
            }

            tableData.AppendLine(string.Format("</td>"));

            return tableData.ToString();
        }

        private string TdTagData(IEnumerable<string> TagData, bool FancyFormat, string TagName, Dictionary<string, string> TagClasses)
        {
            var tableData = new StringBuilder();

            if (TagData != null && TagData.Any())
            {
                tableData.AppendLine(string.Format("<td>"));
                if (FancyFormat)
                {
                    foreach (var tag in TagData)
                    {
                        var tagHtml = "";
                        var tagClass = "label label-default";

                        var tagUrl = Dragonfly.NetHelpers.Url.AppendQueryStringToUrl(Request.RequestUri, TagName, tag);

                        if (TagClasses != null)
                        {
                            var match = TagClasses.ContainsKey(tag) ? TagClasses[tag] : "";
                            if (match != "")
                            {
                                tagClass = $"label label-{match}";
                            }
                        }

                        tagHtml = $"<a href=\"{tagUrl}\"><span class=\"{tagClass}\">{tag}</span></a> ";

                        tableData.AppendLine(tagHtml);
                    }
                }
                else
                {
                    var tagsString = string.Join(", ", TagData);
                    tableData.AppendLine(tagsString);
                }

                tableData.AppendLine(string.Format("</td>"));
            }
            else
            { tableData.AppendLine(string.Format("<td></td>")); }

            return tableData.ToString();
        }

        private string TdDateData(DateTime DateData, bool FancyFormat)
        {
            var tableData = new StringBuilder();

            if (DateData != DateTime.MinValue)
            {
                tableData.AppendLine(string.Format("<td>{0}</td>", DateData));
            }
            else
            { tableData.AppendLine(string.Format("<td></td>")); }

            return tableData.ToString();
        }

        private string TdBoolData(bool BooleanData, bool FancyFormat, string TrueHtml, string FalseHtml)
        {
            var tableData = new StringBuilder();

            if (BooleanData)
            {
                tableData.AppendLine($"<td>{TrueHtml}</td>");
            }
            else
            {
                tableData.AppendLine($"<td>{FalseHtml}</td>");
            }

            return tableData.ToString();
        }

        private string TdImage(MediaImage Image, bool FancyFormat)
        {
            var tableData = new StringBuilder();

            if (Image.Url != "")
            {
                tableData.AppendLine($"<td><img src=\"{Image.Url}\" width=\"300\" /></td>");
            }
            else
            { tableData.AppendLine(string.Format("<td></td>")); }

            return tableData.ToString();
        }

        private string TdStringData(string DataText, bool FancyFormat)
        {
            var tableData = new StringBuilder();

            tableData.AppendLine($"<td>{DataText}</td>");

            return tableData.ToString();
        }

        private string TdStringData(int DataText, bool FancyFormat)
        {
            return TdStringData(DataText.ToString(), FancyFormat);
        }

        private string TdStringData(IHtmlString DataText, bool FancyFormat)
        {
            return TdStringData(DataText.ToString(), FancyFormat);
        }

        private string TdNestedContent(IPublishedProperty Property, bool FancyFormat)
        {
            var tableData = new StringBuilder();

            var ncItems = Property.GetValue<IEnumerable<IPublishedContent>>().ToList();

            tableData.AppendLine($"<td>");

            if (ncItems.Any())
            {
                //start list
                tableData.AppendLine($"<ol>");

                foreach (var item in ncItems)
                {
                    tableData.AppendLine($"<li>{item.Name}</li>");
                }

                //end list
                tableData.AppendLine($"</ol>");
            }
            else
            {
                tableData.AppendLine($"<i>No items added</i>");
            }

            tableData.AppendLine($"</td>");

            return tableData.ToString();
        }

        private string TdMntp(IPublishedProperty Property, bool FancyFormat)
        {
            var tableData = new StringBuilder();

            var items = Property.GetValue<IEnumerable<IPublishedContent>>().ToList();

            tableData.AppendLine($"<td>");

            if (items.Any())
            {
                //start list
                tableData.AppendLine($"<ol>");

                foreach (var item in items)
                {
                    tableData.AppendLine($"<li>{item.Name} ({item.DocumentTypeAlias})</li>");
                }

                //end list
                tableData.AppendLine($"</ol>");
            }
            else
            {
                tableData.AppendLine($"<i>No items added</i>");
            }

            tableData.AppendLine($"</td>");

            return tableData.ToString();
        }

        private string TdPropertyData(IPublishedProperty Property, IDataTypeDefinition DataType, bool FancyFormat)
        {
            var editor = DataType.PropertyEditorAlias;
            var dbType = DataType.DatabaseType;

            //Special handling based on editor
            switch (editor)
            {
                case "Umbraco.NestedContent":
                    return TdNestedContent(Property, FancyFormat);
                case "Umbraco.MultiNodeTreePicker2":
                    return TdMntp(Property, FancyFormat);
            }

            //If we get here, handle based on DB type
            switch (dbType)
            {
                case DataTypeDatabaseType.Date:
                    return TdDateData(Property.GetValue<DateTime>(), FancyFormat);

                case DataTypeDatabaseType.Decimal:
                    return TdStringData(Property.GetValue<string>(), FancyFormat);

                case DataTypeDatabaseType.Integer:
                    return TdStringData(Property.GetValue<string>(), FancyFormat);

                case DataTypeDatabaseType.Nvarchar:
                    return TdStringData(Property.GetValue<string>(), FancyFormat);

                case DataTypeDatabaseType.Ntext:
                    return TdStringData(Property.GetValue<string>(), FancyFormat);

            }

            //If we get here, who knows what happened, just use string
            return TdStringData(Property.GetValue<string>(), FancyFormat);
        }


        //private string TdXX(bool FancyFormat)
        //{
        //    var tableData = new StringBuilder();


        //    return tableData.ToString();
        //}

        #endregion

        #region Tests & Examples

        /// /Umbraco/backoffice/Api/AuthorizedApi/Test
        [System.Web.Http.AcceptVerbs("GET")]
        public bool Test()
        {
            //LogHelper.Info<AuthorizedApiController>("Test STARTED/ENDED");
            return true;
        }

        /// /Umbraco/backoffice/Api/AuthorizedApi/ExampleReturnHtml
        [System.Web.Http.AcceptVerbs("GET")]
        public HttpResponseMessage ExampleReturnHtml()
        {
            var returnSB = new StringBuilder();

            returnSB.AppendLine("<h1>Hello! This is HTML</h1>");
            returnSB.AppendLine("<p>Use this type of return when you want to exclude &lt;XML&gt;&lt;/XML&gt; tags from your output and don\'t want it to be encoded automatically.</p>");

            return new HttpResponseMessage()
            {
                Content = new StringContent(
                    returnSB.ToString(),
                    Encoding.UTF8,
                    "text/html"
                )
            };
        }

        /// /Umbraco/backoffice/Api/AuthorizedApi/ExampleReturnJson
        [System.Web.Http.AcceptVerbs("GET")]
        public HttpResponseMessage ExampleReturnJson()
        {
            var returnSB = new StringBuilder();

            var testData = new StatusMessage(true, "This is a test object so you can see JSON!");
            string json = JsonConvert.SerializeObject(testData);

            returnSB.AppendLine(json);

            return new HttpResponseMessage()
            {
                Content = new StringContent(
                    returnSB.ToString(),
                    Encoding.UTF8,
                    "application/json"
                )
            };
        }

        /// /Umbraco/backoffice/Api/SiteAuditorApi/ExampleReturnCsv
        [System.Web.Http.AcceptVerbs("GET")]
        public HttpResponseMessage ExampleReturnCsv()
        {
            var returnSB = new StringBuilder();
            var tableData = new StringBuilder();

            for (int i = 0; i < 10; i++)
            {
                tableData.AppendFormat(
                    "\"{0}\",{1},\"{2}\",{3}{4}",
                    "Name " + i,
                    i,
                    string.Format("Some text about item #{0} for demo.", i),
                    DateTime.Now,
                    Environment.NewLine);
            }
            returnSB.Append(tableData);

            return Dragonfly.NetHelpers.Http.StringBuilderToFile(returnSB, "Example.csv");
        }


        #endregion

    }

}
