using UnityEngine;
using UnityEngine.InputSystem;

namespace CoopBeam {
// Controles táctiles responsivos: 1 joystick multifunción 2D por jugador (salto arriba, bajar abajo, movimiento)
// y 1 botón de Superpoder con icono para cada jugador en pantalla única.
public sealed class GameDuoMobileInput : MonoBehaviour {
    public struct State {
        public float axis;
        public bool jump, down, ultimate, action, dash;
        public bool jumpPressed, downPressed, dashPressed, ultimatePressed, actionPressed;
    }

    static GameDuoMobileInput instance;
    static Texture2D disc, ring, powerIcon, chevron;

    readonly State[] current = new State[2], previous = new State[2];
    readonly Vector2[] visualKnob = new Vector2[2];
    struct Capture {
        public int? pointer;
        public bool seen;
    }

    readonly Capture[] sticks = new Capture[2], supers = new Capture[2];
    GameDuoLan.LanMode inputMode;

    const float RefHeight = 540f;
    const float StickTravel = 52f;
    const float BaseRadius = 68f;
    const float KnobRadius = 26f;
    const float SuperRadius = 42f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() {
        instance = null;
        disc = ring = powerIcon = chevron = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install() {
        if (instance) return;
        var go = new GameObject("GameDuo / Controles táctiles");
        instance = go.AddComponent<GameDuoMobileInput>();
        DontDestroyOnLoad(go);
    }

    void Awake() {
        if (Application.isMobilePlatform) {
            Screen.orientation = ScreenOrientation.LandscapeLeft;
        }
    }

    public static State Get(int player) {
        if (GameDuoLan.Mode == GameDuoLan.LanMode.Host && player == 1) {
            return instance ? instance.current[1] : default;
        }
        return instance ? instance.current[Mathf.Clamp(player, 0, 1)] : default;
    }

    // The campaign already renders mouse-testable touch controls in the Editor.
    // Use the matching prompts there even without a simulated touchscreen.
    public static bool TouchControls => Application.isMobilePlatform || Touchscreen.current != null ||
        GameDuoLan.Mode != GameDuoLan.LanMode.Local ||
        Application.isEditor && (GameDuoCampaign.InCampaignScene ||
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().path == GameDuoMainMenu.ScenePath);

    static bool Playable => !GameDuoCinema.Busy && !GameDuoTutorial.BlocksGameplay && !GameDuoCampaign.MissionWon && (
        (GameManager.Instance && GameManager.Instance.players != null && GameManager.Instance.players.Length == 2) ||
        (GameDuoCampaign.Active && GameDuoCampaign.Instance.Stage == 3)
    );

    // Escalado responsivo y coordenadas virtuales independientes de la relación de aspecto
    public static float Scale => Screen.height > 0 ? Screen.height / RefHeight : 1f;
    public static float VirtualWidth => Screen.width > 0 ? Screen.width / Scale : 960f;

    public static Vector2 ScreenToVirtual(Vector2 screen) {
        float s = Scale;
        return new Vector2(screen.x / s, (Screen.height - screen.y) / s);
    }

    public static void GetLayout(out Vector2 stick0, out Vector2 super0, out Vector2 stick1, out Vector2 super1) {
        float s = Scale;
        Rect safe = Screen.safeArea;
        if (safe.width <= 0f || safe.height <= 0f) safe = new Rect(0f, 0f, Screen.width, Screen.height);
        GetLayout(GameDuoLan.Mode, safe.xMin / s, safe.xMax / s, (Screen.height - safe.yMin) / s,
            out stick0, out super0, out stick1, out super1);
    }

    static void GetLayout(GameDuoLan.LanMode mode, float left, float right, float bottom,
        out Vector2 stick0, out Vector2 super0, out Vector2 stick1, out Vector2 super1) {
        float stickY = bottom - 122f;
        if (mode != GameDuoLan.LanMode.Local) {
            stick0 = stick1 = new Vector2(left + 112f, stickY);
            super0 = super1 = new Vector2(right - 112f, stickY);
            return;
        }

        float superInset = Mathf.Min(215f, (right - left) * 0.5f - SuperRadius - 10f);
        float superY = stickY - 110f;
        stick0 = new Vector2(left + 112f, stickY);
        stick1 = new Vector2(right - 112f, stickY);
        super0 = new Vector2(left + superInset, superY);
        super1 = new Vector2(right - superInset, superY);
    }

    void ResetInput() {
        System.Array.Clear(sticks, 0, sticks.Length);
        System.Array.Clear(supers, 0, supers.Length);
        System.Array.Clear(visualKnob, 0, visualKnob.Length);
        current[0] = current[1] = previous[0] = previous[1] = default;
    }

    void OnApplicationPause(bool paused) {
        if (!paused) return;
        ResetInput();
        if (GameDuoLan.Mode == GameDuoLan.LanMode.Guest) GameDuoLan.Send(default);
    }

    void Update() {
        var mode = GameDuoLan.Mode;
        if (inputMode != mode) {
            ResetInput();
            inputMode = mode;
        }
        if (!Playable && mode != GameDuoLan.LanMode.Guest) {
            ResetInput();
            return;
        }

        BeginInputFrame();
        if (mode == GameDuoLan.LanMode.Host) current[1] = GameDuoLan.Remote;

        GetLayout(out Vector2 stick0, out Vector2 super0, out Vector2 stick1, out Vector2 super1);
        var touchscreen = Touchscreen.current;
        if (touchscreen != null) {
            var touches = touchscreen.touches;
            for (int t = 0; t < touches.Count; t++) {
                var touch = touches[t];
                if (!touch.press.isPressed) ReleasePointer(t);
            }
            for (int t = 0; t < touches.Count; t++) {
                var touch = touches[t];
                if (!touch.press.isPressed) continue;
                TrackPointer(t, touch.press.wasPressedThisFrame, ScreenToVirtual(touch.position.ReadValue()),
                    mode, stick0, super0, stick1, super1);
            }
        }

        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.isPressed) {
            TrackPointer(-1, mouse.leftButton.wasPressedThisFrame, ScreenToVirtual(mouse.position.ReadValue()),
                mode, stick0, super0, stick1, super1);
        }

        EndInputFrame();
        if (mode == GameDuoLan.LanMode.Guest) GameDuoLan.Send(current[1]);
    }

    void BeginInputFrame() {
        for (int i = 0; i < 2; i++) {
            previous[i] = current[i];
            current[i] = default;
            sticks[i].seen = supers[i].seen = false;
        }
    }

    void EndInputFrame() {
        for (int i = 0; i < 2; i++) {
            if (!sticks[i].seen) {
                sticks[i] = default;
                visualKnob[i] = Vector2.MoveTowards(visualKnob[i], Vector2.zero, Time.unscaledDeltaTime * 350f);
            }
            if (!supers[i].seen) supers[i] = default;
            Edges(i);
        }
    }

    void ReleasePointer(int pointer) {
        for (int i = 0; i < 2; i++) {
            if (sticks[i].pointer == pointer) sticks[i] = default;
            if (supers[i].pointer == pointer) supers[i] = default;
        }
    }

    void TrackPointer(int pointer, bool pressed, Vector2 position, GameDuoLan.LanMode mode,
        Vector2 stick0, Vector2 super0, Vector2 stick1, Vector2 super1) {
        if (pressed) ReleasePointer(pointer);
        if (mode != GameDuoLan.LanMode.Guest)
            TrackPlayer(pointer, pressed, position, 0, stick0, super0);
        if (mode != GameDuoLan.LanMode.Host)
            TrackPlayer(pointer, pressed, position, 1, stick1, super1);
    }

    void TrackPlayer(int pointer, bool pressed, Vector2 position, int player, Vector2 stick, Vector2 super) {
        Vector2 delta = position - stick;
        if (sticks[player].pointer == pointer ||
            (pressed && !sticks[player].pointer.HasValue && delta.sqrMagnitude <= BaseRadius * BaseRadius)) {
            sticks[player] = new Capture { pointer = pointer, seen = true };
            Vector2 knob = Vector2.ClampMagnitude(delta, StickTravel);
            visualKnob[player] = knob;
            float normX = knob.x / (StickTravel * 0.72f);
            current[player].axis = Mathf.Abs(normX) > 0.12f ? Mathf.Clamp(normX, -1f, 1f) : 0f;
            current[player].jump = knob.y < -StickTravel * 0.36f;
            current[player].down = knob.y > StickTravel * 0.36f;
        } else if (!GameDuoDanceActive && (supers[player].pointer == pointer ||
            (pressed && !supers[player].pointer.HasValue && (position - super).sqrMagnitude <= SuperRadius * SuperRadius))) {
            supers[player] = new Capture { pointer = pointer, seen = true };
            current[player].ultimate = true;
        }
    }

    void Edges(int i) {
        var s = current[i];
        var p = previous[i];
        s.jumpPressed = s.jump && !p.jump;
        s.downPressed = s.down && !p.down;
        s.ultimatePressed = s.ultimate && !p.ultimate;
        s.actionPressed = s.action && !p.action;
        s.dashPressed = s.dash && !p.dash;
        current[i] = s;
    }

    static bool GameDuoDanceActive => GameDuoCampaign.Active && CoopInteractionDirector.Active && CoopInteractionDirector.Active.Dancing;

    void OnGUI() {
        if (!Playable && GameDuoLan.Mode != GameDuoLan.LanMode.Guest) return;
        if (Touchscreen.current == null && GameDuoLan.Mode == GameDuoLan.LanMode.Local && !Application.isEditor) return;

        var oldMatrix = GUI.matrix;
        float s = Scale;
        GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
        GUI.depth = -80;

        GetLayout(out Vector2 s0, out Vector2 sup0, out Vector2 s1, out Vector2 sup1);

        int from = GameDuoLan.Mode == GameDuoLan.LanMode.Guest ? 1 : 0;
        int to = (GameDuoLan.Mode == GameDuoLan.LanMode.Local || GameDuoLan.Mode == GameDuoLan.LanMode.Guest) ? 2 : 1;

        var game = GameManager.Instance;
        var powers = game ? game.GetComponent<LinkPowers>() : null;
        for (int i = from; i < to; i++) {
            Vector2 stick = i == 0 ? s0 : s1;
            Vector2 sup = i == 0 ? sup0 : sup1;
            Color playerColor = i == 0 ? new Color(1f, 0.45f, 0.65f, 0.9f) : new Color(0.45f, 1f, 0.75f, 0.9f);

            DrawJoystick(stick, i, playerColor);
            if (!GameDuoDanceActive) {
                bool charged = powers && powers.Charge >= LinkPowers.ChargeRequired;
                bool confirmed = charged && powers.IsConfirmed(i);
                DrawSuperButton(sup, current[i].ultimate || confirmed, playerColor);
                if (charged) CoopPromptUI.Ring(sup, SuperRadius + 4f, 1, confirmed ? CoopPromptUI.Mint : playerColor, 3);
            }
        }

        if (GameDuoLan.Mode == GameDuoLan.LanMode.Host) {
            CoopPromptUI.Text(new Rect(15, 15, 370, 28), GameDuoLan.Status, 13, CoopPromptUI.Mint);
        } else if (GameDuoLan.Mode == GameDuoLan.LanMode.Guest &&
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != GameDuoMainMenu.ScenePath) {
            CoopPromptUI.Text(new Rect(VirtualWidth * 0.5f - 170f, 16, 340, 30), GameDuoLan.Status, 16, CoopPromptUI.Mint);
        }

        GUI.matrix = oldMatrix;
    }

    void DrawJoystick(Vector2 center, int player, Color color) {
        // Base circular translúcida y aro del joystick (se mantiene círculo perfecto en cualquier pantalla)
        Circle(center, BaseRadius, new Color(0.06f, 0.05f, 0.12f, 0.55f));
        DrawRing(center, BaseRadius, color * 0.45f);

        // Indicadores direccionales alrededor de la base
        bool upActive = current[player].jump;
        bool downActive = current[player].down;
        bool leftActive = current[player].axis < -0.3f;
        bool rightActive = current[player].axis > 0.3f;

        DrawChevron(center + new Vector2(0, -44f), 0f, upActive ? Color.white : color * 0.55f);
        DrawChevron(center + new Vector2(0, 44f), 180f, downActive ? Color.white : color * 0.55f);
        DrawChevron(center + new Vector2(-44f, 0), 270f, leftActive ? Color.white : color * 0.55f);
        DrawChevron(center + new Vector2(44f, 0), 90f, rightActive ? Color.white : color * 0.55f);

        // Knob (palanca 2D móvil con seguimiento táctil suave)
        Vector2 knobPos = center + visualKnob[player];
        Circle(knobPos, KnobRadius + 3f, new Color(0.04f, 0.03f, 0.08f, 0.75f));
        Circle(knobPos, KnobRadius, color);
        Circle(knobPos, KnobRadius * 0.42f, Color.white * 0.85f);
    }

    void DrawSuperButton(Vector2 center, bool pressed, Color color) {
        float r = pressed ? SuperRadius * 0.94f : SuperRadius;

        // Fondo del botón
        Circle(center, r, new Color(0.06f, 0.05f, 0.12f, 0.65f));
        if (pressed) {
            Circle(center, r - 3f, color * 0.45f);
        }
        DrawRing(center, r, pressed ? Color.white : color);

        // Icono de superpoder (rayo / energía estilizado)
        float iconSize = r * 1.15f;
        Color iconColor = pressed ? new Color(1f, 0.92f, 0.4f, 1f) : Color.white;
        DrawTextureCentered(center, iconSize, iconSize, PowerIcon(), iconColor);
    }

    static void Circle(Vector2 c, float r, Color color) {
        var old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(new Rect(c.x - r, c.y - r, r * 2f, r * 2f), Disc());
        GUI.color = old;
    }

    static void DrawRing(Vector2 c, float r, Color color) {
        var old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(new Rect(c.x - r, c.y - r, r * 2f, r * 2f), RingTex());
        GUI.color = old;
    }

    static void DrawChevron(Vector2 c, float angleDeg, Color color) {
        var oldMat = GUI.matrix;
        GUIUtility.RotateAroundPivot(angleDeg, c);
        var oldCol = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(new Rect(c.x - 10f, c.y - 10f, 20f, 20f), ChevronTex());
        GUI.color = oldCol;
        GUI.matrix = oldMat;
    }

    static void DrawTextureCentered(Vector2 c, float w, float h, Texture2D tex, Color color) {
        var old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(new Rect(c.x - w * 0.5f, c.y - h * 0.5f, w, h), tex);
        GUI.color = old;
    }

    static Texture2D Disc() {
        if (disc) return disc;
        disc = new Texture2D(64, 64, TextureFormat.RGBA32, false) {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        for (int y = 0; y < 64; y++) {
            for (int x = 0; x < 64; x++) {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f));
                disc.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01((31.5f - dist) * 0.85f)));
            }
        }
        disc.Apply();
        return disc;
    }

    static Texture2D RingTex() {
        if (ring) return ring;
        ring = new Texture2D(64, 64, TextureFormat.RGBA32, false) {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        for (int y = 0; y < 64; y++) {
            for (int x = 0; x < 64; x++) {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f));
                float ringDist = Mathf.Abs(dist - 28.5f);
                float alpha = Mathf.Clamp01((3.2f - ringDist) * 1.1f);
                ring.SetPixel(x, y, new Color(1, 1, 1, alpha));
            }
        }
        ring.Apply();
        return ring;
    }

    static Texture2D ChevronTex() {
        if (chevron) return chevron;
        chevron = new Texture2D(32, 32, TextureFormat.RGBA32, false) {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        for (int y = 0; y < 32; y++) {
            for (int x = 0; x < 32; x++) {
                float dx = Mathf.Abs(x - 15.5f);
                float dy = Mathf.Abs(y - (7f + dx * 1.35f));
                float alpha = (y >= 7 && y <= 24 && dx <= 12f) ? Mathf.Clamp01((2.4f - dy) * 1.5f) : 0f;
                chevron.SetPixel(x, y, new Color(1, 1, 1, alpha));
            }
        }
        chevron.Apply();
        return chevron;
    }

    internal static Texture2D PowerIcon() {
        if (powerIcon) return powerIcon;
        int size = 64;
        powerIcon = new Texture2D(size, size, TextureFormat.RGBA32, false) {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        Vector2[] poly = {
            new Vector2(0.56f, 0.90f),
            new Vector2(0.28f, 0.48f),
            new Vector2(0.48f, 0.48f),
            new Vector2(0.38f, 0.10f),
            new Vector2(0.72f, 0.56f),
            new Vector2(0.52f, 0.56f)
        };
        Color[] cols = new Color[size * size];
        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                float hits = 0;
                for (float dy = -0.3f; dy <= 0.3f; dy += 0.3f) {
                    for (float dx = -0.3f; dx <= 0.3f; dx += 0.3f) {
                        float nx = (x + 0.5f + dx) / size;
                        float ny = (y + 0.5f + dy) / size;
                        if (InsidePoly(nx, ny, poly)) hits += 1f;
                    }
                }
                cols[y * size + x] = new Color(1, 1, 1, hits / 9f);
            }
        }
        powerIcon.SetPixels(cols);
        powerIcon.Apply();
        return powerIcon;
    }

    static bool InsidePoly(float x, float y, Vector2[] poly) {
        bool inside = false;
        int n = poly.Length;
        for (int i = 0, j = n - 1; i < n; j = i++) {
            if (((poly[i].y > y) != (poly[j].y > y)) &&
                (x < (poly[j].x - poly[i].x) * (y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)) {
                inside = !inside;
            }
        }
        return inside;
    }
}
}
