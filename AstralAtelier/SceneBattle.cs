using Vortice.Mathematics;

internal sealed class SceneBattle : IDisposable
{
    private static readonly Rect MainButton = new(300, 535, 360, 64);
    private static readonly Rect RetryButton = new(230, 520, 240, 64);
    private static readonly Rect TitleButton = new(490, 520, 240, 64);
    private static readonly Rect MenuButton = new(24, 586, 170, 34);
    private static readonly Rect GameOverNewGameButton = new(350, 430, 262, 74);
    private static readonly Rect GameOverExitButton = new(350, 515, 262, 85);
    private static readonly Rect[] ActionButtons =
    [
        new(90, 520, 240, 92),
        new(360, 520, 240, 92),
        new(630, 520, 240, 92)
    ];

    private readonly GameSession _game = new(2);
    private readonly string[] _enemyNames = [UiText.Get("enemy_obmon"), UiText.Get("enemy_lavamon")];
    private readonly G2Texture?[] _enemies = new G2Texture?[2];
    private readonly G2Texture?[] _actionIcons = new G2Texture?[3];
    private G2Texture? _background;
    private G2Texture? _academy;
    private G2Texture? _player;
    private G2Texture? _deadPlayer;
    private G2Texture? _gameOverScreen;
    private G2Texture? _gameClearScreen;
    private GameUi? _ui;

    public BgmTrack Music => _game.Phase switch
    {
        PlayPhase.Academy or PlayPhase.GameClear => BgmTrack.Title,
        PlayPhase.GameOver => BgmTrack.GameOver,
        _ => BgmTrack.Battle
    };

    public void Initialize()
    {
        _background = new G2Texture("resource/background/dungueon.png");
        _academy = new G2Texture("resource/background/academy.png");
        _player = new G2Texture("resource/character/charaterbattle.png");
        _deadPlayer = new G2Texture("resource/character/charaterdead.png");
        _gameOverScreen = new G2Texture("resource/background/gameover.png");
        _gameClearScreen = new G2Texture("resource/background/gameclear.png");
        _enemies[0] = new G2Texture("resource/enemy/obmon.png");
        _enemies[1] = new G2Texture("resource/enemy/lavamon.png");
        _actionIcons[0] = new G2Texture("resource/ui/choice/attack.png");
        _actionIcons[1] = new G2Texture("resource/ui/choice/defense.png");
        _actionIcons[2] = new G2Texture("resource/ui/choice/buff.png");
        _ui = new GameUi();
    }

    public void StartNewGame() => _game.StartNewGame();

    public bool Update()
    {
        G2InputContext input = G2AppBase.Instance!.Input;
        if (input.IsKeyDown(Keys.Escape))
        {
            return true;
        }

        if (!input.IsButtonDown(MouseButtons.Left))
        {
            return false;
        }

        System.Drawing.PointF mouse = input.MousePosition;
        switch (_game.Phase)
        {
            case PlayPhase.Academy:
                if (GameUi.Contains(MainButton, mouse))
                {
                    _game.EnterDungeon();
                }
                else if (GameUi.Contains(MenuButton, mouse))
                {
                    return true;
                }
                break;
            case PlayPhase.Battle:
                for (int i = 0; i < ActionButtons.Length; i++)
                {
                    if (GameUi.Contains(ActionButtons[i], mouse))
                    {
                        _game.SelectAction((MagicAction)i);
                        break;
                    }
                }
                break;
            case PlayPhase.RoundResult:
                if (GameUi.Contains(MainButton, mouse))
                {
                    _game.ContinueAfterRound();
                }
                break;
            case PlayPhase.Victory:
                if (GameUi.Contains(MainButton, mouse))
                {
                    _game.ReturnToAcademy();
                }
                break;
            case PlayPhase.GameOver:
                if (GameUi.Contains(GameOverNewGameButton, mouse))
                {
                    _game.StartNewGame();
                }
                else if (GameUi.Contains(GameOverExitButton, mouse))
                {
                    G2AppBase.Instance!.Close();
                }
                break;
            case PlayPhase.GameClear:
                if (GameUi.Contains(RetryButton, mouse))
                {
                    _game.StartNewGame();
                }
                else if (GameUi.Contains(TitleButton, mouse))
                {
                    return true;
                }
                break;
        }

        return false;
    }

    public void Render()
    {
        if (_game.Phase == PlayPhase.GameOver)
        {
            RenderGameOver();
            return;
        }

        if (_game.Phase == PlayPhase.GameClear)
        {
            RenderGameClear();
            return;
        }

        if (_game.Phase == PlayPhase.Academy)
        {
            RenderAcademy();
            return;
        }

        RenderBattle();
        switch (_game.Phase)
        {
            case PlayPhase.Battle:
                RenderActions();
                break;
            case PlayPhase.RoundResult:
                RenderRoundResult();
                break;
            case PlayPhase.Victory:
                RenderVictory();
                break;
        }
    }

    private void RenderAcademy()
    {
        GameUi ui = _ui!;
        Draw(_academy, new Rect(0, 0, 960, 640));
        ui.Fill(new Rect(0, 0, 960, 640), new Color4(0.03f, 0.04f, 0.1f, 0.32f));
        RenderHeader(UiText.Get("academy_title"), UiText.Get("academy_hint"));

        ui.Fill(new Rect(245, 170, 470, 316), GameUi.PanelColor);
        ui.Border(new Rect(245, 170, 470, 316), GameUi.Gold);
        ui.Center(UiText.Get("goal_label"), new Rect(270, 191, 420, 36), GameUi.Muted);
        ui.Heading(UiText.Get("academy_credits", _game.Credits, GameSession.TargetCredits), new Rect(370, 235, 230, 52), GameUi.Gold);
        ui.Center(UiText.Get("defeated_total", _game.EnemiesDefeated), new Rect(270, 294, 420, 38));
        ui.Center(UiText.Get("reward_hint"), new Rect(270, 342, 420, 36));
        ui.Center(UiText.Get("reset_hint"), new Rect(260, 385, 440, 36), GameUi.Muted);
        ui.Center(UiText.Get("clear_hint"), new Rect(270, 428, 420, 32), GameUi.Gold);

        ui.Button(MainButton, UiText.Get("button_dungeon"), GameUi.Gold);
        ui.Button(MenuButton, UiText.Get("button_title"), GameUi.Muted);
        ui.Text(UiText.Get("escape_hint"), new Rect(785, 600, 160, 24), GameUi.Muted, small: true);
    }

    private void RenderHeader(string title, string subtitle)
    {
        GameUi ui = _ui!;
        ui.Fill(new Rect(24, 18, 912, 88), GameUi.PanelColor);
        ui.Heading(title, new Rect(44, 27, 640, 42));
        ui.Text(subtitle, new Rect(44, 74, 655, 25), GameUi.Muted, small: true);
        ui.Center(UiText.Get("credits_short", _game.Credits, GameSession.TargetCredits), new Rect(730, 30, 186, 40), GameUi.Gold);
        ui.Center(UiText.Get("defeated_short", _game.EnemiesDefeated), new Rect(730, 69, 186, 25), GameUi.Muted);
    }

    private void RenderBattle()
    {
        GameUi ui = _ui!;
        Draw(_background, new Rect(0, 0, 960, 640));
        RenderHeader(UiText.Get("battle_title", _game.EncounterNumber, _enemyNames[_game.CurrentEnemyIndex]),
            _game.Phase == PlayPhase.Battle ? UiText.Get("battle_hint") : UiText.Get("turn_number", _game.RoundNumber));

        ui.Fill(new Rect(24, 114, 326, 98), GameUi.PanelColor);
        ui.Fill(new Rect(610, 114, 326, 98), GameUi.PanelColor);
        RenderStatus(UiText.Get("player"), _game.Player, 42, GameUi.Green);
        RenderStatus(UiText.Get("enemy"), _game.Enemy, 628, GameUi.Pink);

        G2Texture? player = _game.Player.Hp == 0 ? _deadPlayer : _player;
        Draw(player, new Rect(52, 211, 285, 285));
        Draw(_enemies[_game.CurrentEnemyIndex], new Rect(585, 211, 320, 280));
        ui.Fill(new Rect(0, 484, 960, 156), GameUi.PanelColor);
        ui.Center(UiText.Get("battle_guide"),
            new Rect(24, 486, 912, 29), GameUi.Muted);
    }

    private void RenderStatus(string name, Combatant character, float x, Color4 color)
    {
        GameUi ui = _ui!;
        ui.Text(UiText.Get("hp_status", name, character.Hp, character.MaxHp), new Rect(x, 121, 294, 27));
        ui.HpBar(character, new Rect(x, 154, 290, 14), color);
        ui.Text(UiText.Get("effect_status", character.Shield, character.AttackBonus),
            new Rect(x, 177, 294, 28), GameUi.Muted, small: true);
    }

    private void RenderActions()
    {
        string[] descriptions =
        [
            UiText.Get("attack_hint", _game.Player.AttackPower),
            UiText.Get("defense_hint", _game.Player.DefensePower),
            UiText.Get("buff_hint", _game.Player.BuffPower)
        ];
        Color4[] colors = [GameUi.Pink, GameUi.Blue, GameUi.Green];
        for (int i = 0; i < ActionButtons.Length; i++)
        {
            Rect button = ActionButtons[i];
            _ui!.Button(button, "", colors[i]);
            Draw(_actionIcons[i], new Rect(button.X + 14, button.Y + 17, 58, 58));
            _ui.Text(GameUi.ActionName((MagicAction)i), new Rect(button.X + 91, button.Y + 12, 135, 30), colors[i]);
            _ui.Text(descriptions[i], new Rect(button.X + 91, button.Y + 48, 145, 28), GameUi.White, small: true);
        }
    }

    private void RenderRoundResult()
    {
        GameUi ui = _ui!;
        BattleRound round = _game.LastRound!;
        RenderResultPanel(UiText.Get("round_title"), GameUi.White);
        ui.Text(UiText.Get("player_action", GameUi.ActionName(round.PlayerAction)), new Rect(266, 262, 435, 31), GameUi.Green);
        ui.Text(DescribeAction(round.PlayerAction, round.DamageToEnemy, _game.Player),
            new Rect(266, 296, 435, 31), GameUi.White, small: true);
        ui.Text(round.EnemyAction.HasValue ? UiText.Get("enemy_action", GameUi.ActionName(round.EnemyAction.Value)) : UiText.Get("enemy_defeated"),
            new Rect(266, 340, 435, 31), GameUi.Pink);
        ui.Text(round.EnemyAction.HasValue
                ? DescribeAction(round.EnemyAction.Value, round.DamageToPlayer, _game.Enemy)
                : UiText.Get("enemy_cannot_act"),
            new Rect(266, 374, 435, 31), GameUi.White, small: true);

        string buttonText = UiText.Get(_game.Enemy.Hp == 0 || _game.Player.Hp == 0 ? "button_battle_result" : "button_next_turn");
        ui.Button(MainButton, buttonText, GameUi.Gold);
    }

    private static string DescribeAction(MagicAction action, int damage, Combatant actor) => action switch
    {
        MagicAction.Attack => UiText.Get("attack_result", damage),
        MagicAction.Defense => UiText.Get("defense_result", actor.DefensePower),
        MagicAction.Buff => UiText.Get("buff_result", actor.BuffPower),
        _ => throw new ArgumentOutOfRangeException(nameof(action))
    };

    private void RenderVictory()
    {
        GameUi ui = _ui!;
        RenderResultPanel(UiText.Get("victory_title"), GameUi.Gold);
        ui.Center(UiText.Get("credit_reward", _game.LastCreditReward), new Rect(245, 269, 470, 50), GameUi.Gold);
        ui.Center(UiText.Get("credits_total", _game.Credits, GameSession.TargetCredits), new Rect(245, 332, 470, 42));
        ui.Center(UiText.Get("victory_hint"), new Rect(245, 390, 470, 31), GameUi.Muted);
        ui.Button(MainButton, UiText.Get("button_academy"), GameUi.Gold);
    }

    private void RenderGameClear()
    {
        GameUi ui = _ui!;
        Draw(_gameClearScreen, new Rect(0, 0, 960, 640));
        ui.Center(UiText.Get("clear_screen_summary", _game.Credits, GameSession.TargetCredits, _game.EnemiesDefeated),
            new Rect(180, 478, 600, 34), GameUi.Gold);
        ui.Button(RetryButton, UiText.Get("button_retry"), GameUi.Green);
        ui.Button(TitleButton, UiText.Get("button_title"), GameUi.Blue);
    }

    private void RenderGameOver()
    {
        _ui!.Fill(new Rect(0, 0, 960, 640), new Color4(0, 0, 0, 1));
        Rect source = _gameOverScreen!.SourceRectangle;
        float width = source.Width * 640 / source.Height;
        Draw(_gameOverScreen, new Rect((960 - width) / 2, 0, width, 640));
        _ui.Center(UiText.Get("gameover_summary", _game.Credits, GameSession.TargetCredits, _game.EnemiesDefeated),
            new Rect(220, 252, 520, 30), GameUi.Muted);

        System.Drawing.PointF mouse = G2AppBase.Instance!.Input.MousePosition;
        if (GameUi.Contains(GameOverNewGameButton, mouse))
        {
            _ui.Border(GameOverNewGameButton, GameUi.Gold, 2);
        }
        else if (GameUi.Contains(GameOverExitButton, mouse))
        {
            _ui.Border(GameOverExitButton, GameUi.Gold, 2);
        }
    }

    private void RenderResultPanel(string title, Color4 color)
    {
        _ui!.Fill(new Rect(232, 221, 496, 218), GameUi.PanelColor);
        _ui.Border(new Rect(232, 221, 496, 218), color, 2);
        _ui.Heading(title, new Rect(264, 226, 430, 44), color);
    }

    private static void Draw(G2Texture? texture, Rect destination)
    {
        texture?.Draw(destination, texture.SourceRectangle);
    }

    public void Dispose()
    {
        _ui?.Dispose();
        foreach (G2Texture? texture in _actionIcons)
        {
            texture?.Dispose();
        }
        foreach (G2Texture? texture in _enemies)
        {
            texture?.Dispose();
        }
        _deadPlayer?.Dispose();
        _gameOverScreen?.Dispose();
        _gameClearScreen?.Dispose();
        _player?.Dispose();
        _academy?.Dispose();
        _background?.Dispose();
    }
}
