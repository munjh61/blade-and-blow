using Game.Domain;
using UnityEngine;

namespace UI.Play
{
    public struct KillEvent
    {
        public string killerName;
        public string victimName;
        public Sprite weaponIcon;
        public bool teamKill;
        public TeamId killerTeam;
        public TeamId victimTeam;
    }
}