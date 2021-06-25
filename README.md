This WebAPI is for use with EngageIP to allow for Device Status to be shown in the EngageIP Admin Portal.

![Example of Usage](https://github.com/cchance27/cnMaestroDeviceStatusEIP/blob/master/sample%20cnMaestro%20Integration%20v1.PNG?raw=true)

Copy the .dll to the bin/ directory of the AdminPortal

Update your web.config to adad the new cnApi path attributes and settings.
	Url must be accessible from EngageIP server specifically.
	Api ClientID and Secret can be generated in EngageIP API Panel

```xml
<configuration>
  <appSettings>
    <add key="cnMaestroApiUrl" value = "https://cnmaestro/api/v1" /> 
    <add key="cnMaestroClientID" value = "from-cnmaestro-interface" />
    <add key="cnMaestroClientSecret" value = "from-cnmaestro-interface" />
  </appSettings>
  <system.web>
    <httpHandlers>
      <add verb="GET" path="*.cnApi" type="cnMaestro.API, cnMaestro" />
    </httpHandlers>
  </system.web>
  <system.webServer>
    <handlers accessPolicy="Read, Script">
      <remove name="cnmaestro" />
      <add name="cnmaestro" path="*.cnApi" verb="GET" type="cnMaestro.API, cnMaestro" modules="IsapiModule" scriptProcessor="C:\Windows\Microsoft.NET\Framework64\v4.0.30319\aspnet_isapi.dll" resourceType="Unspecified" requireAccess="None" preCondition="classicMode,runtimeVersionv4.0,bitness64" />
    </handlers>
  </system.webServer>
</configuration>
```

Once Completed you should now be able to make calls to :
	https://engageip/adminportal/0a-00-e3-00-00-00.cnmaestro

You can view the page extension javascript example that's also included in this repo as an example of including this information in the AdminPortal on all Cambium Packages.