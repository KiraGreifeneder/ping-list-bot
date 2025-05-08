# ping-list-bot

Asynchronously pings a list of IP addresses to check for availability. 
If an address is reachable, it will attempt to send a HTTP request to port 80 and 8080.

## Getting started

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

## Implementation

The ping bot is implemented as a pipeline where the all the blue blocks in the graphic below run in parallel.

> Warning: Currently, there is no limit as to how many of each block can run in parallel. Don't overdo it :D

IP addresses enter the input buffer as strings and are converted to an IPAddressStatus object. 
This object is passed along the pipeline and has its fields set depending on the success or failure of the ping or request.
An address is fed back into the ping block 5 times (causing another ping attempt to be made for it) before giving up.
If a successful ping was made, the address is fed right into the `TryHTTP` blocks, where requests are made on port 80 and 8080 respectively.

The final ActionBlock runs synchronously again and handles all the console output once the `IPAdressStatus` objects arrive there. 
This is not parallelized, mainly because it simplifies two things:

- writing to the console
- writing to the list that the final objects are stored in easier.

This diagram contains all the blocks and paths the data can go through within the pipeline.

![image](https://github.com/user-attachments/assets/6a1d24ec-d826-4257-ab5a-69b8ee3ba413)
