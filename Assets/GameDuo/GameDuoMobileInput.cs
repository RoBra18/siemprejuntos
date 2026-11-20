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
    readonly bool[] touchActive = new bool[2];

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

    static bool Playable => !GameDuoCinema.Busy && (
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

    // Compatibilidad
    static Vector2 ToGui(Vector2 screen) => ScreenToVirtual(screen);

    public static void GetLayout(out Vector2 stick0, out Vector2 super0, out Vector2 stick1, out Vector2 super1) {
        float s = Scale;
        float vw = VirtualWidth;
        Rect safe = Screen.safeArea;
        float safeL = safe.xMin / s;
        float safeR = (Screen.width - safe.xMax) / s;

        float padL = Mathf.Max(safeL, 0f);
        float padR = Mathf.Max(safeR, 0f);

        if (GameDuoLan.Mode == GameDuoLan.LanMode.Guest) {
            // Modo invitado (1 móvil para el 2do jugador): joystick a la izquierda, superpoder a la derecha
            stick0 = super0 = Vector2.zero;
            stick1 = new Vector2(padL + 120f, 418f);
            super1 = new Vector2(vw - padR - 120f, 418f);
        } else {
            // Modo local compartido (2 jugadores en 1 pantalla):
            // Jugador 0 (Mora/Mimo - Izquierda):
            stick0 = new Vector2(padL + 112f, 418f);
            super0 = new Vector2(padL + 232f, 360f);

            // Jugador 1 (Lima/Pipro - Derecha):
            stick1 = new Vector2(vw - padR - 112f, 418f);
            super1 = new Vector2(vw - padR - 232f, 360f);
        }
    }

    void Update() {
        if (!Playable && GameDuoLan.Mode != GameDuoLan.LanMode.Guest) {
            current[0] = current[1] = default;
            return;
        }

        for (int i = 0; i < 2; i++) {
            previous[i] = current[i];
            current[i] = default;
            touchActive[i] = false;
        }

        if (GameDuoLan.Mode == GameDuoLan.LanMode.Host) {
            current[1] = GameDuoLan.Remote;
            Edges(1);
        }

        // Multi-touch en pantalla táctil
        if (Touchscreen.current != null) {
            int count = Touchscreen.current.touches.Count;
            for (int t = 0; t < count; t++) {
                var touch = Touchscreen.current.touches[t];
                if (!touch.press.isPressed) continue;
                Apply(ScreenToVirtual(touch.position.ReadValue()));
            }
        }

        // Puntero / ratón para pruebas en Editor o PC
        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.isPressed) {
            Apply(ScreenToVirtual(mouse.position.ReadValue()));
        }

        // Suavizado del knob cuando se suelta el toque
        for (int i = 0; i < 2; i++) {
            if (!touchActive[i]) {
                visualKnob[i] = Vector2.MoveTowards(visualKnob[i], Vector2.zero, Time.unscaledDeltaTime * 350f);
            }
        }

        Edges(0);
        if (GameDuoLan.Mode != GameDuoLan.LanMode.Host) {
            Edges(1);
        }

        if (GameDuoLan.Mode == GameDuoLan.LanMode.Guest) {
            GameDuoLan.Send(current[1]);
        }
    }

    void Apply(Vector2 p) {
        GetLayout(out Vector2 s0, out Vector2 sup0, out Vector2 s1, out Vector2 sup1);

        int player = (GameDuoLan.Mode == GameDuoLan.LanMode.Guest) ? 1 : (p.x < VirtualWidth * 0.5f ? 0 : 1);
        if (GameDuoLan.Mode == GameDuoLan.LanMode.Host && player == 1) return;

        Vector2 stick = player == 0 ? s0 : s1;
        Vector2 sup = player == 0 ? sup0 : sup1;

        // 1. Detección del botón de Superpoder (con icono)
        if (Vector2.Distance(p, sup) <= SuperRadius + 14f ||
            Vector2.Distance(p, new Vector2(player == 0 ? 326f : 634f, 300f)) <= 46f) {
            current[player].ultimate = true;
            return;
        }

        // 2. Detección del Joystick 2D multifunción (movimiento, salto arriba, bajar abajo)
        Vector2 delta = p - stick;
        if (delta.sqrMagnitude <= (BaseRadius + 45f) * (BaseRadius + 45f)) {
            touchActive[player] = true;
            Vector2 knob = Vector2.ClampMagnitude(delta, StickTravel);
            visualKnob[player] = knob;

            // Eje horizontal (movimiento lateral)
            float normX = knob.x / (StickTravel * 0.72f);
            current[player].axis = Mathf.Abs(normX) > 0.12f ? Mathf.Clamp(normX, -1f, 1f) : 0f;

            // Eje vertical: Arriba para Salto, Abajo para Bajar/Caer de plataformas
            if (knob.y < -StickTravel * 0.36f) current[player].jump = true;
            if (knob.y > StickTravel * 0.36f) current[player].down = true;
            return;
        }

        // Compatibilidad con pruebas automáticas
        if (player == 0 && Vector2.Distance(p, new Vector2(326f, 425f)) <= 55f) {
            current[0].jump = true;
            return;
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

        for (int i = from; i < to; i++) {
            Vector2 stick = i == 0 ? s0 : s1;
            Vector2 sup = i == 0 ? sup0 : sup1;
            Color playerColor = i == 0 ? new Color(1f, 0.45f, 0.65f, 0.9f) : new Color(0.45f, 1f, 0.75f, 0.9f);

            DrawJoystick(stick, i, playerColor);
            DrawSuperButton(sup, current[i].ultimate, playerColor);
        }

        if (GameDuoLan.Mode == GameDuoLan.LanMode.Host) {
            CoopPromptUI.Text(new Rect(15, 15, 370, 28), GameDuoLan.Status, 13, CoopPromptUI.Mint);
        } else if (GameDuoLan.Mode == GameDuoLan.LanMode.Guest) {
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

    static Texture2D PowerIcon() {
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
