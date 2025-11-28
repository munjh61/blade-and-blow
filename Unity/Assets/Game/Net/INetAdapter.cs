using System;
using Game.Domain;

namespace Game.Net
{
    public interface INetAdapter
    {
        bool IsMine { get; }
        int OwnerId { get; }
        void PublishState(PlayerState s);
        event Action<PlayerState> OnState;

        void RpcAll(string method, params object[] args);
    }
}
