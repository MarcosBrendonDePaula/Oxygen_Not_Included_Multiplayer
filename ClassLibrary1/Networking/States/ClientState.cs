using System;

namespace ONI_MP.Networking.States
{
    public enum ClientState
    {
        Error = -1,
        Disconnected,
        Connecting,
        Connected,
        SyncingMods,    // Estado enquanto sincroniza mods
        SyncModsFailed, // (Opcional) Caso falhe na sincronização
        LoadingWorld,
        InGame
    }

    [Flags]
    public enum ClientReadyState
    {
        Ready,
        Unready
    }
}
