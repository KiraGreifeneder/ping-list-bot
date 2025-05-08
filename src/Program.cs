using Core.Services;

var provider = new FileIPAddressProvider(Path.GetFullPath("iplist.txt"));
var pingListBot = new PingListBot(provider);
Console.WriteLine("Starting");
await pingListBot.StartAsync();
pingListBot.PrintSummary();