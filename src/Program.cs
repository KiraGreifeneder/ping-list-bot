using Core.Services;

if (args.Length == 0)
{
    Console.WriteLine("Please provide the path to the IP list file as a command-line argument.");
    return;
}

var provider = new FileIPAddressProvider(Path.GetFullPath(args[0]));
var pingListBot = new PingListBot(provider);
Console.WriteLine("Starting");
await pingListBot.StartAsync();
pingListBot.PrintSummary();