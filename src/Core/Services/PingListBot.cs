namespace Core.Services;

using System.Net.NetworkInformation;
using System.Threading.Tasks.Dataflow;
using Core.Interfaces;
using Core.Models;

public sealed class PingListBot : IDisposable
{
    private readonly int MAX_PING_ATTEMPTS = 5;

    private readonly IIPAddressProvider _provider;
    private readonly List<IPAddressStatus> _results;
    private readonly HttpClient _httpClient;

    // ---------- Dataflow blocks ----------
    private readonly BufferBlock<string> _inputBuffer;
    private readonly TransformBlock<string, IPAddressStatus> _stringToIPDataClass;
    private readonly TransformBlock<IPAddressStatus, IPAddressStatus> _getPingStatus;
    private readonly TransformBlock<IPAddressStatus, IPAddressStatus> _getHttp80Status;
    private readonly TransformBlock<IPAddressStatus, IPAddressStatus> _getHttp8080Status;
    private readonly ActionBlock<IPAddressStatus> _handleOutputs;

    // ---------- Dataflow options ----------
    private readonly DataflowLinkOptions _linkOptions = new() { PropagateCompletion = true };
    private readonly ExecutionDataflowBlockOptions _blockOptions = new() { MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded };

    public PingListBot(IIPAddressProvider provider)
    {
        _provider = provider;
        _results = [];
        _httpClient = new HttpClient();

        _inputBuffer = new BufferBlock<string>();
        _stringToIPDataClass = new TransformBlock<string, IPAddressStatus>(ip => new IPAddressStatus(ip), _blockOptions);
        _getPingStatus = new TransformBlock<IPAddressStatus, IPAddressStatus>(PingIPAsync, _blockOptions);
        _getHttp80Status = new TransformBlock<IPAddressStatus, IPAddressStatus>(GetHttp80Async, _blockOptions);
        _getHttp8080Status = new TransformBlock<IPAddressStatus, IPAddressStatus>(GetHttp8080Async, _blockOptions);
        _handleOutputs = new ActionBlock<IPAddressStatus>(HandleOutputs);


        _inputBuffer.LinkTo(_stringToIPDataClass, _linkOptions);
        _stringToIPDataClass.LinkTo(_getPingStatus, _linkOptions);

        _getPingStatus.LinkTo(_getHttp80Status, _linkOptions, ip => ip.IsReachable); // if reachable, skip to checking http statuses
        _getPingStatus.LinkTo(_handleOutputs); // skip to end if not reachable

        _getHttp80Status.LinkTo(_getHttp8080Status, _linkOptions);
        _getHttp8080Status.LinkTo(_handleOutputs, _linkOptions);
    }

    // Start reading the IP addresses from the provider and sending them through the pipeline
    public async Task StartAsync()
    {
        _results.Clear();
        var ips = _provider.GetIPAddresses();
        await Task.WhenAll(ips.Select(ip => _inputBuffer.SendAsync(ip)));
        _inputBuffer.Complete();
        await _handleOutputs.Completion; // Wait for the entire pipeline to finish
    }

    private void HandleOutputs(IPAddressStatus ip)
    {
        _results.Add(ip);
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss:fffff}] - IP: {ip.Address}");
        Console.WriteLine($"  Reachable: {ip.IsReachable}");
        Console.WriteLine($"  Pings: {ip.Pings}");
        Console.WriteLine($"  HTTP 80: {(ip.Http80IsOpen ? "open" : "closed")}");
        Console.WriteLine($"  HTTP 8080: {(ip.Http8080IsOpen ? "open" : "closed")}");
    }

    public void PrintSummary()
    {
        if (_results.Count == 0)
        {
            Console.WriteLine("No IP addresses were processed.");
            return;
        }

        // print a table with the results
        Console.WriteLine("\n===================== Summary of ping results =====================");
        Console.WriteLine($"{"IP Address",-20} {"Reachable",-10} {"Pings",-10} {"HTTP 80",-10} {"HTTP 8080",-10}");
        Console.WriteLine(new string('=', 70));
        foreach (var result in _results)
        {
            var reachable = result.IsReachable ? "yes" : "no";
            var http80 = result.Http80IsOpen ? "open" : "closed";
            var http8080 = result.Http8080IsOpen ? "open" : "closed";
            Console.WriteLine($"{result.Address,-20} {reachable,-10} {result.Pings,-10} {http80,-10} {http8080,-10}");
        }
        Console.WriteLine(new string('=', 70));
        Console.WriteLine($"Total IP addresses processed: {_results.Count}");
        Console.WriteLine($"Total reachable IP addresses: {_results.Count(r => r.IsReachable)}");
        Console.WriteLine($"Total HTTP 80 open IP addresses: {_results.Count(r => r.Http80IsOpen)}");
        Console.WriteLine($"Total HTTP 8080 open IP addresses: {_results.Count(r => r.Http8080IsOpen)}");
        Console.WriteLine("===================================================================");
    }

    private async Task<IPAddressStatus> PingIPAsync(IPAddressStatus ip)
    {
        while (ip.Pings < MAX_PING_ATTEMPTS)
        {
            using var ping = new Ping();

            try
            {
                var result = await ping.SendPingAsync(ip.Address, 1000);
                ip.Pings++;
                if (result.Status == IPStatus.Success)
                {
                    ip.IsReachable = true;
                    return ip;
                }
            }
            catch (Exception) 
            {
                // Ignored — another attempt can be made or it is marked as unreachable later.
            }
        }

        ip.IsReachable = false;
        return ip;
    }

    private async Task<IPAddressStatus> GetHttp80Async(IPAddressStatus ip)
    {
        ip.Http80IsOpen = await HttpIsOpenAsync(ip, 80);
        return ip;
    }

    private async Task<IPAddressStatus> GetHttp8080Async(IPAddressStatus ip)
    {
        ip.Http8080IsOpen = await HttpIsOpenAsync(ip, 8080);
        return ip;
    }

    private async Task<bool> HttpIsOpenAsync(IPAddressStatus ip, int port)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            var response = await _httpClient.GetAsync($"http://{ip.Address}:{port}", cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}