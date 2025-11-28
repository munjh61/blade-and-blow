using UnityEngine;

public class UserInfo
{

    [SerializeField] private int maxHp;
    [SerializeField] private int Hp;
    [SerializeField] private int kill;
    [SerializeField] private int death;
    [SerializeField] private int damage;
    [SerializeField] private int consecutiveKill;

    public int MaxHp { get { return maxHp; } }
    public int CurrentHp { get { return Hp; } }
    public int Kills { get { return kill; } }
    public int Deaths { get { return death; } }
    public int Damage { get { return damage; } }
    public int ConsecutiveKill { get { return consecutiveKill; } }

    public UserInfo(int maxHp, int Hp, int kill, int death, int damage, int consecutiveKill) { 
        

    
    }



}
