namespace Core.Interfaces;

public interface IIPAddressProvider 
{
    IEnumerable<string> GetIPAddresses();
}