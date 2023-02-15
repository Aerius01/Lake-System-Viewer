using System.Net.NetworkInformation;   
using System.Net;   
using System.Threading.Tasks;

public class Pinger
{
    // A simple ping is enough since we will have already confirmed by this point whether the DB exists.
    // We're simply wondering about whether the bridge to this address is still up.

    private IPAddress host;

    public Pinger(IPAddress host) { this.host = host; }

    public bool PingHost()
    {
        bool pingable = false;
        Ping pinger = null;

        try
        {
            pinger = new Ping();
            PingReply reply = pinger.Send(this.host);
            pingable = reply.Status == IPStatus.Success;
        }
        catch (PingException) { ; } // Discard PingExceptions and return false
        finally { if (pinger != null) { pinger.Dispose(); } }

        return pingable;
    }

    public async Task<bool> PingHostAsync()
    {
        bool pingable = false;
        Ping pinger = null;

        try
        {
            pinger = new Ping();
            PingReply reply = await pinger.SendPingAsync(this.host);
            pingable = reply.Status == IPStatus.Success;
        }
        catch (PingException) { ; } // Discard PingExceptions and return false
        finally { if (pinger != null) { pinger.Dispose(); } }

        return pingable;
    }
}