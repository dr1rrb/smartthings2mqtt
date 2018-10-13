# SmartThings 2 MQTT
Exposes properties and lets you control your SmartThings devices through MQTT

# Install
## SmartApp
1. Login to https://graph.api.smartthings.com
1. Go to  _My SmartApps_, click _Settings_, then _Add new repository_
1. Configure the following
	- Owner: dr1rrb
	- Name: smartthings2mqtt
	- Branch: master
1. Clisk _Save_
1. Click _Update from Repo_ and select "smartthings2mqtt" in the list
1. Select the "src/mqtt-adapter.groovy", then check _Publish_ and finally click _Execute Update_
1. On you phone, open the SmartThings application, open _Automation/SmartApps_
1. At the bottom of the list, sleect _Add s SmartApp_
1. Select _My Apps/MQTT adapater_
1. Select all the devices you want to expose to MQTT ans then click _Save_

## Bridge
The bridge is running in a linux container:
```
docker run -it --name="smartthings2mqtt" -p 1983:1983 -v /docker/smartthings2mqtt:/smartthings2mqtt dr1rrb/smartthings2mqtt
```

# Configuration
Add a file _/smartthings2mqtt/config.json_ with content:
```json
{
	"SmartThings": {
		"LocationId": "<id>"
	},
	"Bridge": {
		"TopicNamespace": "smartthings",
		"BridgeToStAuthToken": "<token>",
		"StToBridgeAuthToken": "<token>"
	},
	"Broker": {
		"Host": "<IP_of_your_broker>",
		"Port": 1883,
		"Username": null,
		"Pasword": null,
		"ClientId": "SmartThings2MQTT"
	},
	"LogLevel": "Information"
}
```

## SmartThings section
### LocationId _(Required)_
This is the ID of the location you want to bridge.
1. In SmartThings IDE, go to _My Locations_ (https://graph.api.smartthings.com/location/list)
1. Select your location
1. Get the ID from the Url

## Bridge section
### TopicNamespace _(Optional)_
This is the prefix used for all topics created and listen by the bridge.
_Default: smartthings_

### BridgeToStAuthToken _(Required)_
This is the authorization token used by the MQTT bridge to send update to the SmartApp
1. In SmartThings IDE, go to _My Locations_ (https://graph.api.smartthings.com/location/list)
1. Select your location
1. Go to _List SmartApps_ 
1. Search for _MQTT adapter_  and open it
1. At the bottom, in the _Application State_, the token is the _accessToken_

### StToBridgeAuthToken _(Required)_
This is the authorization token used by the SmartApp to send update to the MQTT bridge. 
You can configure it in the _SmartApp_ settings.

## Broker section
### Host _(Required)_
The hostname of the broker 
**Due to a bug in .net core, you cannot use a hostname, but instead you have to put the IP address** (https://github.com/dotnet/corefx/issues/24355)

### Port _(Optional)_
The port of the broker
_Default: 1883_

### Username _(Optional)_
The username to use to connect to the broker

### Password _(Optional)_
The password to use to connect to the broker

### ClientId _(Optional)_
The client ID to use to connect to the broker
_Default: {MachineName}\_SmartThings2MQTT_
