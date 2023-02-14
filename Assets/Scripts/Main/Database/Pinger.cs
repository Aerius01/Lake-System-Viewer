using System.Net.NetworkInformation;   
using System.Net;   

public class Pinger
{
    // A simple ping is enough since we will have already confirmed by this point whether the DB exists.
    // We're simply wondering about whether the bridge to this address is still up.

    private IPAddress host;

    public Pinger(IPAddress host) { this.host = host; }

    public bool PingHost()
    {
        bool pingable = false;
        System.Net.NetworkInformation.Ping pinger = null;

        try
        {
            pinger = new System.Net.NetworkInformation.Ping();
            PingReply reply = pinger.Send(this.host);
            pingable = reply.Status == IPStatus.Success;
        }
        catch (PingException) { ; } // Discard PingExceptions and return false
        finally { if (pinger != null) { pinger.Dispose(); } }

        return pingable;
    }
}