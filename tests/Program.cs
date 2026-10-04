internal static class Program
{
    private static int _checks;

    private static void Main()
    {
        CheckAttackDefenseBuff();
        CheckVictoryAndEnemyRotation();
        CheckClearAndRestart();
        CheckGameOver();
        CheckRewardRange();
        Console.WriteLine($"PASS: {_checks} combat and game-flow checks.");
    }

    private static void CheckAttackDefenseBuff()
    {
        Combatant player = new(100, 20, 15, 20);
        Combatant enemy = new(60, 12, 10, 8);
        Equal(20, player.Act(MagicAction.Attack, enemy), "base attack");
        player.Act(MagicAction.Defense, enemy);
        Equal(0, enemy.Act(MagicAction.Attack, player), "defense cannot heal or cause negative damage");
        Equal(0, player.Shield, "shield consumed by one attack");
        Equal(12, enemy.Act(MagicAction.Attack, player), "later attack is unguarded");

        player.Act(MagicAction.Buff, enemy);
        player.Act(MagicAction.Buff, enemy);
        Equal(20, player.AttackBonus, "buff does not stack");
        player.Act(MagicAction.Defense, enemy);
        Equal(20, player.AttackBonus, "buff persists across defense");
        enemy.Act(MagicAction.Defense, player);
        Equal(30, player.Act(MagicAction.Attack, enemy), "buffed attack minus enemy defense");
        Equal(0, player.AttackBonus, "attack consumes buff");
        Equal(0, enemy.Shield, "attack consumes enemy shield");
        Equal(10, player.Act(MagicAction.Attack, enemy), "damage capped at remaining HP");
        Equal(0, enemy.Hp, "HP never negative");

        player.Reset();
        enemy.Reset();
        player.Act(MagicAction.Defense, enemy);
        enemy.Act(MagicAction.Buff, player);
        Equal(15, player.Shield, "shield remains when enemy buffs");
        Equal(5, enemy.Act(MagicAction.Attack, player), "enemy buff and player shield combine");
        Equal(0, enemy.AttackBonus, "enemy attack consumes enemy buff");
        player.Reset();
        Equal(100, player.Hp, "reset restores HP");
        Equal(0, player.Shield, "reset clears shield");
        Equal(0, player.AttackBonus, "reset clears buff");
    }

    private static void CheckVictoryAndEnemyRotation()
    {
        GameSession game = new(2, new FixedRandom(3));
        Equal(PlayPhase.Academy, game.Phase, "new game academy");
        game.EnterDungeon();
        Equal(0, game.CurrentEnemyIndex, "first enemy");
        game.SelectAction(MagicAction.Attack);
        Equal(PlayPhase.RoundResult, game.Phase, "result locks actions");
        Equal(88, game.Player.Hp, "enemy responds after player");
        game.SelectAction(MagicAction.Attack);
        Equal(40, game.Enemy.Hp, "double selection ignored");
        game.ContinueAfterRound();
        game.SelectAction(MagicAction.Attack);
        game.ContinueAfterRound();
        game.SelectAction(MagicAction.Attack);
        Equal<MagicAction?>(null, game.LastRound!.EnemyAction, "defeated enemy cannot act");
        Equal(76, game.Player.Hp, "no retaliation after lethal attack");
        Equal(0, game.Credits, "reward pending result confirmation");
        game.ContinueAfterRound();
        Equal(PlayPhase.Victory, game.Phase, "victory below target");
        Equal(3, game.Credits, "reward added");
        Equal(1, game.EncounterNumber, "victory keeps completed encounter number");
        game.ContinueAfterRound();
        Equal(3, game.Credits, "reward cannot be collected twice");
        game.ReturnToAcademy();
        game.EnterDungeon();
        Equal(1, game.CurrentEnemyIndex, "second enemy");
        Equal(2, game.EncounterNumber, "next encounter number");
        Equal(100, game.Player.Hp, "new encounter restores player HP");
        Equal(60, game.Enemy.Hp, "new encounter restores enemy HP");
    }

    private static void CheckClearAndRestart()
    {
        GameSession game = new(2, new FixedRandom(3));
        for (int encounter = 0; encounter < 4; encounter++)
        {
            game.EnterDungeon();
            Equal(encounter % 2, game.CurrentEnemyIndex, "two enemies repeat in order");
            DefeatEnemy(game);
            if (encounter < 3)
            {
                game.ReturnToAcademy();
            }
        }

        Equal(PlayPhase.GameClear, game.Phase, "10 credits clears game");
        Equal(10, game.Credits, "credits capped at 10");
        Equal(1, game.LastCreditReward, "last reward capped to remaining credit");
        Equal(4, game.EnemiesDefeated, "defeat count");
        game.EnterDungeon();
        Equal(PlayPhase.GameClear, game.Phase, "cannot enter dungeon after clear");
        game.StartNewGame();
        Equal(PlayPhase.Academy, game.Phase, "restart returns to academy");
        Equal(0, game.Credits, "restart resets credits");
        Equal(0, game.EnemiesDefeated, "restart resets enemy sequence");
        Equal<BattleRound?>(null, game.LastRound, "restart clears result");
    }

    private static void CheckGameOver()
    {
        GameSession game = new(2, new FixedRandom(1));
        game.EnterDungeon();
        for (int turn = 0; turn < 9; turn++)
        {
            game.SelectAction(MagicAction.Buff);
            game.ContinueAfterRound();
        }
        Equal(0, game.Player.Hp, "lethal enemy attack");
        Equal(PlayPhase.GameOver, game.Phase, "game over at zero HP");
        Equal(0, game.Credits, "loss gives no credits");
        Equal(0, game.EnemiesDefeated, "loss does not count defeat");
        game.SelectAction(MagicAction.Attack);
        Equal(60, game.Enemy.Hp, "no actions after game over");
        game.StartNewGame();
        Equal(100, game.Player.Hp, "retry restores player HP");
    }

    private static void CheckRewardRange()
    {
        for (int reward = 1; reward <= 3; reward++)
        {
            GameSession game = new(2, new FixedRandom(reward));
            game.EnterDungeon();
            DefeatEnemy(game);
            Equal(reward, game.Credits, "all rewards 1 through 3 supported");
        }
    }

    private static void DefeatEnemy(GameSession game)
    {
        while (game.Phase == PlayPhase.Battle)
        {
            game.SelectAction(MagicAction.Attack);
            game.ContinueAfterRound();
        }
    }

    private static void Equal<T>(T expected, T actual, string description)
    {
        _checks++;
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{description}: expected {expected}, got {actual}");
        }
    }

    private sealed class FixedRandom(int reward) : Random
    {
        public override int Next(int maxValue) => 0;
        public override int Next(int minValue, int maxValue) => reward;
    }
}
