using System;

namespace ONI_MP.Networking.States
{
    public enum ServerState
    {
        Error = -1,
        Stopped,
        Preparing,
        Starting,
        Started,
        WaitingForModSync,  // Aguardando resposta de compatibilidade de mods dos clientes
        ModSyncComplete     // Todos os clientes confirmaram compatibilidade
    }
}
