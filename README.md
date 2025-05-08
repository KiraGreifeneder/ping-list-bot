# ping-list-bot
Pings a list of IP addresses to check for availability. If an address is reachable, it will attempt to send a HTTP request to port 80 and 8080.

Make sure .NET 9 is installed. 

Add the TPL Dataflow package:

```.
dotnet add package System.Threading.Tasks.Dataflow
```

`dotnet run -- PATH_TO_YOUR_FILE` to ping all the addresses in your file.

The file should contain one IP address per line (see `iplist.txt`) as an example. 


Example usage: 

```
dotnet run -- ./iplist.txt
```
