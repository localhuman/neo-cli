
# Neo CLI Notification Server

This fork of neo-cli is used to persist notifications that are emitted by smart contracts into a database.  It also includes a REST interface for querying those events.

## Usage

Use the same as normal neo-cli, with 2 additional optional startup parameters:
- pass `-n` to instruct the client to record notifications to a database
- pass `-s` to serve a REST API for those notifications

Default usage is to start the client as: `dotnet neo-cli.dll -n -s`

It is not recommended to use the RPC server at the same time as the notification server.

## Configuration
2 additional items are present in the `config.json` included with this fork. 
- The `Notifications` key is in the config `Paths` element, and is used to determine a directory where the notification database should reside. 
- The `Host` key is in the config `REST` element, and should be a string of valid host names.  The default of `http://0.0.0.0:80` will bind the REST API to port 80 on the server.

## Prebuilt:

[You can download the linux 64 bit build here](https://s3.us-east-2.amazonaws.com/cityofzion/neo-cli-notifications/NeoCliNotifications_2.7.6.1.zip)

## Bootstraps:

Bootstrap Chain and Notification Database are available here:

- [Chain2567xxx.tar.gz](https://s3.us-east-2.amazonaws.com/cityofzion/neo-cli-notifications/Chain2567xxx.tar.gz)
- [Notif2567xxx.tar.gz](https://s3.us-east-2.amazonaws.com/cityofzion/neo-cli-notifications/Notif2567xxx.tar.gz)
