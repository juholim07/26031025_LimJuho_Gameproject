internal enum MagicAction
{
    Attack,
    Defense,
    Buff
}

internal enum PlayPhase
{
    Academy,
    Battle,
    RoundResult,
    Victory,
    GameOver,
    GameClear
}

internal sealed class Combatant
{
    public int MaxHp { get; }
    public int Hp { get; private set; }
    public int AttackPower { get; }
    public int DefensePower { get; }
    public int BuffPower { get; }
    public int Shield { get; private set; }
    public int AttackBonus { get; private set; }

    public Combatant(int maxHp, int attackPower, int defensePower, int buffPower)
    {
        MaxHp = maxHp;
        AttackPower = attackPower;
        DefensePower = defensePower;
        BuffPower = buffPower;
        Reset();
    }

    public void Reset()
    {
        Hp = MaxHp;
        Shield = 0;
        AttackBonus = 0;
    }

    public int Act(MagicAction action, Combatant target)
    {
        switch (action)
        {
            case MagicAction.Attack:
                int damage = target.ReceiveDamage(AttackPower + AttackBonus);
                AttackBonus = 0;
                return damage;
            case MagicAction.Defense:
                Shield = DefensePower;
                return 0;
            case MagicAction.Buff:
                AttackBonus = BuffPower;
                return 0;
            default:
                throw new ArgumentOutOfRangeException(nameof(action));
        }
    }

    private int ReceiveDamage(int attack)
    {
        int damage = Math.Min(Hp, Math.Max(0, attack - Shield));
        Hp -= damage;
        Shield = 0;
        return damage;
    }
}

internal sealed record BattleRound(
    MagicAction PlayerAction,
    MagicAction? EnemyAction,
    int DamageToEnemy,
    int DamageToPlayer);

internal sealed class GameSession
{
    public const int TargetCredits = 10;

    private readonly Random _random;
    private readonly int _enemyCount;

    public Combatant Player { get; } = new(100, 20, 15, 20);
    public Combatant Enemy { get; } = new(60, 12, 10, 8);
    public PlayPhase Phase { get; private set; }
    public int Credits { get; private set; }
    public int EnemiesDefeated { get; private set; }
    public int CurrentEnemyIndex { get; private set; }
    public int EncounterNumber { get; private set; }
    public int RoundNumber { get; private set; }
    public int LastCreditReward { get; private set; }
    public BattleRound? LastRound { get; private set; }

    public GameSession(int enemyCount, Random? random = null)
    {
        if (enemyCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(enemyCount));
        }

        _enemyCount = enemyCount;
        _random = random ?? new Random();
        StartNewGame();
    }

    public void StartNewGame()
    {
        Credits = 0;
        EnemiesDefeated = 0;
        CurrentEnemyIndex = 0;
        EncounterNumber = 0;
        RoundNumber = 0;
        LastCreditReward = 0;
        LastRound = null;
        Player.Reset();
        Enemy.Reset();
        Phase = PlayPhase.Academy;
    }

    public void EnterDungeon()
    {
        if (Phase != PlayPhase.Academy)
        {
            return;
        }

        CurrentEnemyIndex = EnemiesDefeated % _enemyCount;
        EncounterNumber = EnemiesDefeated + 1;
        Player.Reset();
        Enemy.Reset();
        RoundNumber = 0;
        LastRound = null;
        Phase = PlayPhase.Battle;
    }

    public void SelectAction(MagicAction action)
    {
        if (Phase != PlayPhase.Battle)
        {
            return;
        }

        MagicAction? enemyAction = null;
        int damageToEnemy = Player.Act(action, Enemy);
        int damageToPlayer = 0;

        // 적을 처치한 턴에는 적이 다시 행동하지 않는다.
        if (Enemy.Hp > 0)
        {
            enemyAction = (MagicAction)_random.Next(3);
            damageToPlayer = Enemy.Act(enemyAction.Value, Player);
        }

        RoundNumber++;
        LastRound = new BattleRound(action, enemyAction, damageToEnemy, damageToPlayer);
        Phase = PlayPhase.RoundResult;
    }

    public void ContinueAfterRound()
    {
        if (Phase != PlayPhase.RoundResult)
        {
            return;
        }

        if (Enemy.Hp == 0)
        {
            LastCreditReward = Math.Min(_random.Next(1, 4), TargetCredits - Credits);
            Credits += LastCreditReward;
            EnemiesDefeated++;
            Phase = Credits == TargetCredits ? PlayPhase.GameClear : PlayPhase.Victory;
        }
        else if (Player.Hp == 0)
        {
            Phase = PlayPhase.GameOver;
        }
        else
        {
            Phase = PlayPhase.Battle;
        }
    }

    public void ReturnToAcademy()
    {
        if (Phase == PlayPhase.Victory)
        {
            Phase = PlayPhase.Academy;
        }
    }
}
