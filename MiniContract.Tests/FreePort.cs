using System.Net;
using System.Net.Sockets;

namespace MiniContract.Tests;

/// <summary>Tim cong TCP con trong cho test khoi dong server that (tranh dung cong co dinh giua cac test/agent chay cung luc).</summary>
public static class FreePort
{
    public static int Get()
    {
        var l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        var port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return port;
    }
}
