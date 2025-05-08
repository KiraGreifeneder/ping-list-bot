# ping-list-bot
Pings a list of IP addresses to check for availability.

Make sure .NET 9 is installed. 

Add the TPL Dataflow package:

```
dotnet add package System.Threading.Tasks.Dataflow
```

`dotnet run -- PATH_TO_YOUR_FILE` to ping all the addresses in your file.

The file should contain one IP address per line (see `iplist.txt`) as an example. 


Example usafe: 

```
dotnet run -- ./iplist.txt
```
