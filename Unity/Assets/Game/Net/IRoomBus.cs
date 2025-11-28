using System;
using Game.Domain;

namespace Game.Net
{
    public interface IRoomBus
    {
        void SendTo(PlayerId target, NetEvt evt, object payload, bool reliable = true);

        void Broadcast(NetEvt evt, object payload, bool reliable = true);

        event Action<NetEvt, object, PlayerId> EventReceived;
    }
}
