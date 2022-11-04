# Dragonfly.UmbracoLogViewer #

A tool for storing Umbraco log entries in a database and viewing them in a tabular format created by [Heather Floyd](https://www.HeatherFloyd.com).

## Installation ##
[![Nuget Downloads](https://buildstats.info/nuget/Dragonfly.UmbracoLogViewer)](https://www.nuget.org/packages/Dragonfly.UmbracoLogViewer/)

    PM > Install-Package Dragonfly.UmbracoLogViewer

## Database Setup ##

After installing, you will need to provide the database access information for where Logs should be stored.

You can either store the log data in the current Umbraco DB, or you can use an external database.

### Setup to Use Umbraco DB ###

1. Add AppKeys to your web.config :

		<appSettings>
			...
			<add key="UseUmbracoDb" value="true" />
			<add key="LogDbDSN" value="[connectionstring matching umbracoDbDSN]" />
			...
		</appSettings>

When you restart your site, new tables will be added to handle the logging.


### Setup to Use External DB ###

1. Create an empty external database.

2. Add AppKeys to your web.config :

		 <appSettings>
			 ...
			 <add key="UseUmbracoDb" value="false" />
			 <add key="LogDbDSN" value="server=mydbserver;database=MyLoggingDB;user id=username;password='password!'" />
			 ...
		 </appSettings>

3. Add a matching DB Connection string to your web.config :

		<connectionStrings>
			...
			<add name="LogDbDSN" connectionString="server=mydbserver;database=MyLoggingDB;user id=username;password='password!'" providerName="System.Data.SqlClient" />
			...
		</connectionStrings>

## Configuring the LogAppender ##

For either database option, update your "/config/log4net.config" file with the additional  AdoNetAppender. (See the included "example-log4net.config" for these updates in context.)


Add the reference to the root at the top:

	<root>
		...
		<appender-ref ref="DragonflyAdoNetAppender" />
	</root>

Add the Appender code:

	 <!-- Dragonfly.UmbracoLogViewer Appender-->
	  <appender name="DragonflyAdoNetAppender" type="log4net.Appender.AdoNetAppender">
	    <bufferSize value="1" />
	    <connectionType value="System.Data.SqlClient.SqlConnection, System.Data, Version=1.0.3300.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" />
	    <AppSettingsKey value="LogDbDSN"/>
	    <commandText value="INSERT INTO dbo.LogViewerLog ([Date],[Thread],[Level],[Logger],[Message],[Exception],[Host],[UrlPath],[ProcessUserName],[Properties]) VALUES (@log_date, @thread, @log_level, @logger, @message, @exception, @host, @url, @process_user, @properties)" />
	    <parameter>
	      <parameterName value="@log_date" />
	      <dbType value="DateTime" />
	      <layout type="log4net.Layout.RawTimeStampLayout" />
	    </parameter>
	    <parameter>
	      <parameterName value="@thread" />
	      <dbType value="String" />
	      <size value="255" />
	      <layout type="log4net.Layout.PatternLayout">
	        <conversionPattern value="%thread" />
	      </layout>
	    </parameter>
	    <parameter>
	      <parameterName value="@log_level" />
	      <dbType value="String" />
	      <size value="50" />
	      <layout type="log4net.Layout.PatternLayout">
	        <conversionPattern value="%level" />
	      </layout>
	    </parameter>
	    <parameter>
	      <parameterName value="@logger" />
	      <dbType value="String" />
	      <size value="255" />
	      <layout type="log4net.Layout.PatternLayout">
	        <conversionPattern value="%logger" />
	      </layout>
	    </parameter>
	    <parameter>
	      <parameterName value="@message" />
	      <dbType value="String" />
	      <size value="4000" />
	      <layout type="log4net.Layout.PatternLayout">
	        <conversionPattern value="%message" />
	      </layout>
	    </parameter>
	    <parameter>
	      <parameterName value="@exception" />
	      <dbType value="String" />
	      <size value="2000" />
	      <layout type="log4net.Layout.ExceptionLayout" />
	    </parameter>
	    <parameter>
	      <parameterName value="@host" />
	      <dbType value="String" />
	      <size value="2000" />
	      <layout type="log4net.Layout.PatternLayout">
	        <conversionPattern value="%aspnet-request{HTTP_HOST}" />
	      </layout>
	    </parameter>
	    <parameter>
	      <parameterName value="@url" />
	      <dbType value="String" />
	      <size value="2000" />
	      <layout type="log4net.Layout.PatternLayout">
	        <!--<conversionPattern value="%property{log4net:HostName}" />-->
	        <conversionPattern value="%aspnet-request{URL}" />
	      </layout>
	    </parameter>
	    <parameter>
	      <parameterName value="@process_user" />
	      <dbType value="String" />
	      <size value="255" />
	      <layout type="log4net.Layout.PatternLayout">
	        <conversionPattern value="%property{log4net:UserName}" />
	      </layout>
	    </parameter>
	    <parameter>
	      <parameterName value="@properties" />
	      <dbType value="String" />
	      <size value="4000" />
	      <layout type="log4net.Layout.PatternLayout">
	        <conversionPattern value="%aspnet-context" />
	      </layout>
	    </parameter>
	    <filter type="log4net.Filter.StringMatchFilter">
	      <stringToMatch value="state: Login attempt failed for username"/>
	      <acceptOnMatch value="true" />
	    </filter>
	    <filter type="log4net.Filter.LevelRangeFilter">
	      <levelMin value="WARN" />
	      <acceptOnMatch value="true" />
	    </filter>
	    <filter type="log4net.Filter.DenyAllFilter" />
	  </appender>


Make sure to update the "connectionType" to match the type of database you are using:

**SQL Server**

	    <connectionType value="System.Data.SqlClient.SqlConnection, System.Data, Version=1.0.3300.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" />


**SQL CE**

	    <connectionType value="System.Data.SqlServerCe.SqlCeProviderFactory, System.Data.SqlServerCe" />

### Filtering ###
You can set up filtering to limit what log entries go into the log database. See the [Log4Net documentation](https://logging.apache.org/log4net/release/manual/configuration.html#filters) for details.

I have included filters that limit it to WARN or more critical, and includes failed Umbraco Login attempts.

### Buffer Setting ###

The Buffer setting determines how many log entries are batched before being pushed through the Appender. The larger the number, the more log events have to occur before they will appear in your database. For a Production site, you should increase this number to perhaps 50-100. The default value of "1" means that each event is logged in the database as it occurs. 

	<bufferSize value="1" />

### Troubleshooting ###
If you are not getting data written to your database, turn on the log4net debugger via the web.config:

	<configuration>
	...
		<appSettings>
		 ...
	 		<add key="log4net.Internal.Debug" value="true" />
	    	<add key="log4net.Config.Watch" value="true" />
		...
		</appSettings>
	
		<system.diagnostics>
		    <trace autoflush="true">
		        <listeners>
		            <add 
		                name="textWriterTraceListener" 
		                type="System.Diagnostics.TextWriterTraceListener" 
		                initializeData="App_Data\Logs\Log4NetDebug.txt" />
		        </listeners>
		    </trace>
		</system.diagnostics>
	
	...
	</configuration>

Remember to remove those keys or set them to "false" once you have got everything working the way you want. 

## Viewing The Log Data ##
Once set up, you can view the logs at 

http://Yoursite.com/Umbraco/backoffice/Api/LogViewerApi/LogViewer

*NOTE:* You must be logged-in to the Umbraco back-office in order to view.
