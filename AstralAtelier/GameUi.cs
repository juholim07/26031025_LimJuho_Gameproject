using System.Text.Json;
using Vortice.DirectWrite;
using Vortice.Mathematics;

internal sealed class GameUi : IDisposable
{
    public static readonly Color4 White = new(0.97f, 0.96f, 1, 1);
    public static readonly Color4 Muted = new(0.74f, 0.76f, 0.85f, 1);
    public static readonly Color4 Gold = new(1, 0.83f, 0.44f, 1);
    public static readonly Color4 Pink = new(1, 0.35f, 0.61f, 1);
    public static readonly Color4 Blue = new(0.37f, 0.76f, 1, 1);
    public static readonly Color4 Green = new(0.58f, 0.9f, 0.47f, 1);
    public static readonly Color4 PanelColor = new(0.06f, 0.07f, 0.14f, 0.94f);

    private readonly G2Font _small = new("Malgun Gothic", 16, FontWeight.Normal);
    private readonly G2Font _body = new("Malgun Gothic", 20, FontWeight.SemiBold);
    private readonly G2Font _heading = new("Malgun Gothic", 32);
    private readonly G2Font _center = new("Malgun Gothic", 20, FontWeight.SemiBold,
        textAlignment: TextAlignment.Center, paragraphAlignment: ParagraphAlignment.Center);
    private readonly Dictionary<string, G2Texture> _textures = new();

    public GameUi()
    {
        using JsonDocument catalog = JsonDocument.Parse(
            File.ReadAllText(G2Util.FindFilePath("resource/ui/assets.json")));
        foreach (JsonElement file in catalog.RootElement.GetProperty("textures").EnumerateArray())
        {
            string path = file.GetString()!;
            _textures.Add(path, new G2Texture("resource/ui/" + path));
        }
    }

    public void Fill(Rect rect, Color4 color)
    {
        string path = color.Equals(PanelColor) ? "panels/panel_dark.png" :
            color.Equals(new Color4(0.03f, 0.04f, 0.1f, 0.32f)) ? "panels/academy_overlay.png" :
            color.Equals(new Color4(0, 0, 0, 1)) ? "panels/black.png" :
            color.Equals(new Color4(0.08f, 0.08f, 0.12f, 1)) ? "hp/empty.png" :
            color.Equals(Green) ? "hp/player.png" :
            color.Equals(Pink) ? "hp/enemy.png" :
            throw new ArgumentException("UI fill color has no resource.", nameof(color));
        G2Texture texture = _textures[path];
        texture.Draw(rect, texture.SourceRectangle);
    }

    public void Border(Rect rect, Color4 color, float width = 1)
    {
        string thickness = width <= 1 ? "1" : width <= 1.5f ? "15" : width <= 2 ? "2" : "3";
        DrawNineSlice(_textures[$"frames/{AccentName(color)}_{thickness}.png"], rect);
    }

    public void Text(string text, Rect rect, Color4? color = null, bool small = false)
    {
        (small ? _small : _body).DrawText(text, rect, color ?? White);
    }

    public void Heading(string text, Rect rect, Color4? color = null)
    {
        _heading.DrawText(text, rect, color ?? White);
    }

    public void Center(string text, Rect rect, Color4? color = null)
    {
        _center.DrawText(text, rect, color ?? White);
    }

    public void Button(Rect rect, string text, Color4 accent, bool enabled = true)
    {
        bool hovered = enabled && Contains(rect, G2AppBase.Instance!.Input.MousePosition);
        string name = AccentName(enabled ? accent : Muted);
        string path = $"buttons/button_{name}{(hovered ? "_hover" : "")}.png";
        DrawNineSlice(_textures[path], rect);
        Center(text, rect, enabled ? White : Muted);
    }

    public void HpBar(Combatant character, Rect rect, Color4 color)
    {
        Fill(rect, new Color4(0.08f, 0.08f, 0.12f, 1));
        if (character.Hp > 0)
        {
            Fill(new Rect(rect.X, rect.Y, rect.Width * character.Hp / character.MaxHp, rect.Height), color);
        }
        Border(rect, Muted);
    }

    public static bool Contains(Rect rect, System.Drawing.PointF point)
    {
        return point.X >= rect.X && point.X <= rect.X + rect.Width &&
               point.Y >= rect.Y && point.Y <= rect.Y + rect.Height;
    }

    public static string ActionName(MagicAction action) => action switch
    {
        MagicAction.Attack => UiText.Get("action_attack"),
        MagicAction.Defense => UiText.Get("action_defense"),
        MagicAction.Buff => UiText.Get("action_buff"),
        _ => throw new ArgumentOutOfRangeException(nameof(action))
    };

    public void Dispose()
    {
        foreach (G2Texture texture in _textures.Values)
        {
            texture.Dispose();
        }
        _center.Dispose();
        _heading.Dispose();
        _body.Dispose();
        _small.Dispose();
    }

    private static string AccentName(Color4 color)
    {
        if (color.Equals(White)) return "white";
        if (color.Equals(Muted)) return "muted";
        if (color.Equals(Gold)) return "gold";
        if (color.Equals(Pink)) return "pink";
        if (color.Equals(Blue)) return "blue";
        if (color.Equals(Green)) return "green";
        throw new ArgumentException("UI accent color has no resource.", nameof(color));
    }

    private static void DrawNineSlice(G2Texture texture, Rect destination)
    {
        // 모서리는 유지하고 가운데만 늘려 버튼 크기가 달라도 테두리가 찌그러지지 않는다.
        Rect source = texture.SourceRectangle;
        float sourceCorner = source.Width / 3;
        float corner = Math.Min(sourceCorner, Math.Min(destination.Width, destination.Height) / 2);
        float[] widths = [corner, destination.Width - 2 * corner, corner];
        float[] heights = [corner, destination.Height - 2 * corner, corner];
        float y = destination.Y;
        for (int row = 0; row < 3; row++)
        {
            float x = destination.X;
            for (int column = 0; column < 3; column++)
            {
                if (widths[column] > 0 && heights[row] > 0)
                {
                    texture.Draw(new Rect(x, y, widths[column], heights[row]),
                        new Rect(column * sourceCorner, row * sourceCorner, sourceCorner, sourceCorner));
                }
                x += widths[column];
            }
            y += heights[row];
        }
    }
}
