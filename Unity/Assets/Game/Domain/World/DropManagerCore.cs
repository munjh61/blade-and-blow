using Game.Domain;
using Game.Net;
using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class DropManagerCore
{
    private readonly Dictionary<ulong, (string key, Vector3 pos, Quaternion rot, bool consumed)> _drops = new();

    private Func<PlayerId, Vector3> _getPlayerPos;
    private Func<int, string> _getWeaponKeyOfActor;

    private readonly IRoomBus _bus;
    private readonly INetCodec _codec;

    private bool _isMaster;
    public bool IsMaster => _isMaster;
    public void SetMaster(bool v) => _isMaster = v;

    public event Action<ulong, string, Vector3, Quaternion> OnDropSpawned;
    public event Action<ulong> OnDropRemoved;

    public DropManagerCore(IRoomBus bus, INetCodec codec)
    {
        _bus = bus;
        _codec = codec;

        bus.EventReceived += OnRoomEvent;
    }

    public void SetResolvers(Func<PlayerId, Vector3> getPlayerPos,
                             Func<int, string> getWeaponKeyOfActor)
    {
        _getPlayerPos = getPlayerPos;
        _getWeaponKeyOfActor = getWeaponKeyOfActor;
    }

    public void Dispose() => _bus.EventReceived -= OnRoomEvent;

    public void Master_Spawn(string weaponKey, Vector3 pos, Quaternion rot)
    {
        if (!_isMaster) return;

        ulong token = DropTokenGenerator.New();
        _drops[token] = (weaponKey, pos, rot, false);

        _bus.Broadcast(NetEvt.DropSpawned, _codec.EncodeDropSpawned(token, weaponKey, pos, rot));
    }

    public void Master_HandlePickup(int actor, ulong token)
    {
        if (!_isMaster) return;

        if (!_drops.TryGetValue(token, out var e) || e.consumed) return;

        _drops[token] = (e.key, e.pos, e.rot, true);

        _bus.Broadcast(NetEvt.DropRemoved, _codec.EncodeDropRemoved(token));
    }

    private void OnRoomEvent(NetEvt evt, object payload, PlayerId sender)
    {
        switch (evt)
        {
            case NetEvt.DropSpawned:
            {
                var (token, key, pos, rot) = _codec.DecodeDropSpawned(payload);

                _drops[token] = (key, pos, rot, false);
                OnDropSpawned?.Invoke(token, key, pos, rot);

                break;
            }
            case NetEvt.DropRemoved:
            {
                var token = _codec.DecodeDropRemoved(payload);

                if (_drops.TryGetValue(token, out var e))
                    _drops[token] = (e.key, e.pos, e.rot, true);
                OnDropRemoved?.Invoke(token);

                break;
            }
            case NetEvt.PickupRequest:
            {
                var (actor, token, equipId) = _codec.DecodePickupRequest(payload);
                Master_HandlePickup(actor, token);

                break;
            }
            case NetEvt.DropRequest:
            {
                if (!_isMaster) break;
                int actor = _codec.DecodeDropRequest(payload);

                string key = _getWeaponKeyOfActor?.Invoke(actor);
                if (string.IsNullOrEmpty(key)) break;

                Vector3 pos = _getPlayerPos != null ? _getPlayerPos(new PlayerId(actor)) : Vector3.zero;
                Quaternion rot = Quaternion.identity;

                Master_Spawn(key, pos, rot);

                break;
            }
        }
    }
}

public static class DropTokenGenerator
{
    private static ulong _ctr = 1;
    public static ulong New() => unchecked(++_ctr ^ ((ulong)Environment.TickCount << 32));
}