namespace Core.Models;

public class IPAddressStatus(string address)
{
    public string Address => address; // get-only prop
    public bool IsReachable { get; set; } = false;
    public int Pings { get; set; } = 0;
    public bool Http80IsOpen { get; set; } = false;
    public bool Http8080IsOpen { get; set; } = false;
}

