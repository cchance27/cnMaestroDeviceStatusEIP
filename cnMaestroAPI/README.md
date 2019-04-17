This WebAPI is for use with EngageIP to allow for Device Status to be shown in the EngageIP Admin Portal.

Copy the .dll to the bin/ directory of the AdminPortal

Add the following to <appSettings> in web.config
	Url must be accessible from EngageIP server specifically.
	Api ClientID and Secret can be generated in EngageIP

<add key="cnMaestroApiUrl" value = "https://cnmaestro/api/v1" /> 
<add key="cnMaestroClientID" value = "from-cnmaestro-interface" />
<add key="cnMaestroClientSecret" value = "from-cnmaestro-interface" />

Add the following to enable the handler

	<system.web>
		<httpHandlers>
			<add verb="GET" path="*.cnmaestro" type="cnMaestro.cnProxy, cnMaestroAPI" />
		</httpHandlers>
	</system.web>

Make sure the scriptProcessor is correct, it should be same as the one used for Monorail by engageIP

	<system.webServer>
		<handlers>
            <add name="cnMaestro" path="*.cnmaestro" verb="GET" modules="IsapiModule" scriptProcessor="C:\Windows\Microsoft.NET\Framework64\v2.0.50727\aspnet_isapi.dll" resourceType="Unspecified" requireAccess="Script" />
		</handlers>
	</system.webServer>


Once Completed you should now be able to make calls to :
	https://engageip/adminportal/0a-00-e3-00-00-00.cnmaestro