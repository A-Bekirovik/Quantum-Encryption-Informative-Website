using Microsoft.AspNetCore.SignalR;

namespace Back_End_Quantum_Encryption.Hubs;

/// <summary>
/// Real-time connection with the front-end. The browser calls methods on
/// this hub, and the hub can push messages back to the browser.
/// </summary>
public sealed class Bb84Hub : Hub
{
    /// <summary>Connection test: the front-end calls Ping and gets "pong" back.</summary>
    public string Ping() => "pong";
}
