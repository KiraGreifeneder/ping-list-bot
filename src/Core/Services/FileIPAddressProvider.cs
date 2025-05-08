namespace Core.Services;

using System.Net;
using Core.Interfaces;

public class FileIPAddressProvider(string filePath) : IIPAddressProvider
{
    private readonly string _filePath = filePath;

    public IEnumerable<string> GetIPAddresses()
    {
        if (!File.Exists(_filePath))
        {
            throw new FileNotFoundException("Could not find the specified file.", _filePath);
        }

        foreach (var line in File.ReadLines(_filePath))
        {
            if (IPAddress.TryParse(line, out _))
            {
                yield return line;
            }
        }
    }
}