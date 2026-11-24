using System;
using UnityEngine;

namespace CoopBeam {
public sealed class GameDuoMainMenu : MonoBehaviour {
    public const string ScenePath = "Assets/GameDuo/Scenes/00_Menu.unity";


    enum Page { Home, Missions, Wifi, Privacy }

    static readonly Color Background = new Color(.07f, .06f, .13f);
    static readonly Color Surface = new Color(.13f, .11f, .21f);
    static readonly Color HomeSurface = new Color(.12f, .09f, .23f, .88f);
    static readonly Color HomeBorder = new Color(.66f, .56f, .94f, .82f);
    static readonly Color Border = new Color(.24f, .21f, .35f);
    static readonly Color Ink = new Color(.96f, .93f, 1f);
    static readonly Color Muted = new Color(.73f, .69f, .81f);
    static readonly Color Lavender = new Color(.78f, .68f, 1f);
    static readonly Color Accent = new Color(.61f, .96f, .79f);
    static readonly Color Gold = new Color(1f, .79f, .45f);

    GameDuoTutorial controls;
    AudioSource music;
    Page page = Page.Home;
    int pressedMode = -1;
    float acceptModeInputAt;
    string joinIp = "";
    GUIStyle label;
    Texture homeBackdrop, homeLogo;
    bool loading = true;

    bool IllustratedHome => page == Page.Home && homeBackdrop;

    void Awake() {
        ResetModeInput();
        Time.timeScale = 1;
        AudioListener.pause = false;
        controls = gameObject.AddComponent<GameDuoTutorial>();

        GameDuoLan.Ensure();

        var cameraObject = new GameObject("Menu camera");
        cameraObject.AddComponent<AudioListener>();
        var camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Background;

        music = gameObject.AddComponent<AudioSource>();
        music.playOnAwake = false;
        music.loop = true;
        music.spatialBlend = 0;
        music.ignoreListenerPause = true;
        music.volume = 0;

    }

    System.Collections.IEnumerator Start() {
        var backdrop = Resources.LoadAsync<Texture2D>("SiempreJuntosMenu");
        var logo = Resources.LoadAsync<Texture2D>("SiempreJuntosLogo");
        var theme = Resources.LoadAsync<AudioClip>("MenuTheme");
        yield return backdrop;
        yield return logo;
        yield return theme;
        homeBackdrop = PrepareMenuTexture(backdrop.asset as Texture2D);
        homeLogo = PrepareMenuTexture(logo.asset as Texture2D);
        music.clip = theme.asset as AudioClip;
        if (music.clip) music.Play();
        loading = false;
        ResetModeInput();
    }

    static Texture PrepareMenuTexture(Texture2D source) {
#if UNITY_IOS && !UNITY_EDITOR
        if (source && QualitySettings.activeColorSpace == ColorSpace.Linear) {
            // IMGUI on iOS presents these images with an extra display conversion.
            var target = new RenderTexture(source.width, source.height, 0,
                RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            target.name = source.name + " (iOS menu)";
            target.wrapMode = TextureWrapMode.Clamp;
            if (!target.Create()) {
                Destroy(target);
                return source;
            }

            var previous = RenderTexture.active;
            bool writeSrgb = GL.sRGBWrite;
            try {
                GL.sRGBWrite = false;
                Graphics.Blit(source, target);
            } finally {
                RenderTexture.active = previous;
                GL.sRGBWrite = writeSrgb;
            }
            return target;
        }
#endif
        return source;
    }

    void OnDestroy() {
        if (homeBackdrop is RenderTexture backdrop) Destroy(backdrop);
        if (homeLogo is RenderTexture logo) Destroy(logo);
    }

    void OnApplicationFocus(bool focused) { if (focused) ResetModeInput(); }

    void OnApplicationPause(bool paused) { if (!paused) ResetModeInput(); }

    void ResetModeInput() {
        pressedMode = -1;
        acceptModeInputAt = Time.realtimeSinceStartup + .35f;
    }

    void Update() {
        if (music) music.volume = Mathf.MoveTowards(music.volume, GameDuoCinema.Busy ? 0 : .38f, Time.unscaledDeltaTime * .95f);
    }

    public void StartGame() {
        if (!GameDuoCinema.Busy && GameDuoLan.Mode != GameDuoLan.LanMode.Guest && CampaignEnergy.Current.Count > 0) OpenMission(0);
    }

    internal bool TryStartMission(int stage) {
        if (GameDuoCinema.Busy || GameDuoLan.Mode != GameDuoLan.LanMode.Local ||
            CampaignEnergy.Current.Count == 0 || stage < 0 || stage > GameDuoCampaign.UnlockedStage) return false;
        OpenMission(stage);
        return true;
    }

    static void OpenMission(int stage) {
        GameDuoCinema.Play(stage == 0 ? new[] { 0 } : Array.Empty<int>(), GameDuoCampaign.Scenes[stage]);
    }

    void OnGUI() {
        // The cinema draws the entire screen during playback and transitions.
        if (GameDuoCinema.Busy) return;
        var oldMatrix = GUI.matrix;
        int oldDepth = GUI.depth;
        GUI.depth = 100;
        GUI.matrix = Matrix4x4.identity;
        var viewport = new Rect(0, 0, Screen.width, Screen.height);
        if (IllustratedHome)
            GUI.DrawTexture(viewport, homeBackdrop, ScaleMode.ScaleAndCrop);
        else
            CoopPromptUI.Fill(viewport, Background);
        GUI.matrix = CoopPromptUI.FitSafeArea();

        if (loading) CoopPromptUI.Loading();
        else Header();
        if (!loading && !controls.Reviewing) {
            switch (page) {
                case Page.Home: DrawHome(); break;
                case Page.Missions: DrawMissions(); break;
                case Page.Wifi: DrawWifi(); break;
                case Page.Privacy: DrawPrivacy(); break;
            }
        }

        GUI.matrix = oldMatrix;
        GUI.depth = oldDepth;
    }

    void Header() {
        if (IllustratedHome && homeLogo) {
            GUI.DrawTexture(new Rect(24, 5, 280, 79), homeLogo, ScaleMode.ScaleToFit);
        } else {
            Text(new Rect(24, 20, 305, 43), "Siempre Juntos", 24, Ink, true);
            CoopPromptUI.Fill(new Rect(24, 76, 592, 1), Border);
        }

        var energy = new Rect(477, 23, 139, 33);
        Card(energy, IllustratedHome ? HomeBorder : Border, IllustratedHome ? HomeSurface : Surface);
        CoopPromptUI.Bolt(new Vector2(494, 39.5f), 10, Gold);
        Text(new Rect(513, 23, 61, 33), "Energía", 11, Muted);
        var state = CampaignEnergy.Current;
        Text(new Rect(574, 23, 34, 33), $"{state.Count}/{CampaignEnergy.Capacity}", 13, Gold, true);
        if (state.Count < CampaignEnergy.Capacity) {
            var recharge = new Rect(477, 58, 139, 17);
            Card(recharge, IllustratedHome ? HomeBorder : Border, IllustratedHome ? HomeSurface : Surface);
            Text(recharge, "Próxima en " + state.Countdown, 10, Gold, false, TextAnchor.MiddleCenter);
        }
        if (state.Count == 0 && CampaignRewardedAd.Available && !controls.Reviewing)
            DrawEnergyReward();
    }

    void DrawEnergyReward() {
        var rect = new Rect(312, 23, 154, 42);
        bool busy = CampaignRewardedAd.Busy;
        int retrySeconds = CampaignRewardedAd.RetrySeconds;
        bool canRequest = !busy && retrySeconds == 0;
        bool hovered = canRequest && rect.Contains(Event.current.mousePosition);
        Color surface = IllustratedHome ? HomeSurface : Surface;
        Card(rect, Gold, hovered ? Color.Lerp(surface, Gold, .13f) : surface);
        bool pressed = canRequest && GUI.Button(rect, GUIContent.none, GUIStyle.none);
        var tip = new Vector2(338, 44);
        var top = new Vector2(328, 37);
        var bottom = new Vector2(328, 51);
        CoopPromptUI.Line(top, tip, 1.7f, Gold);
        CoopPromptUI.Line(tip, bottom, 1.7f, Gold);
        CoopPromptUI.Line(bottom, top, 1.7f, Gold);
        string title = busy ? "Cargando…" : retrySeconds > 0 ? "Anuncio no disponible" : CampaignRewardedAd.Failed ? "Reintentar anuncio" : "Ver anuncio";
        Text(new Rect(346, 27, 114, 19), title, 11, Ink, true);
        Text(new Rect(346, 45, 114, 15), retrySeconds > 0 ? $"Reintenta en {retrySeconds} s" : "+1 energía", 10, Gold);
        if (pressed) CampaignRewardedAd.Show(this);
    }

    void DrawHome() {
        DrawHeroArt();
        if (IllustratedHome) Text(new Rect(324, 87, 293, 31), "Campaña", 21, Background, true);
        Text(new Rect(323, 86, 293, 31), "Campaña", 21, Ink, true);
        if (ModeCard(new Rect(323, 123, 293, 67), "Una pantalla", "Dos jugadores en este dispositivo", Accent, false))
            page = Page.Missions;
        if (ModeCard(new Rect(323, 201, 293, 67), "Dos dispositivos", "Jueguen en la misma red Wi‑Fi", Lavender, true))
            page = Page.Wifi;

        CoopPromptUI.Fill(new Rect(24, 310, 592, 1), Border);
        if (Action(new Rect(24, 320, 100, 27), "Controles", Border, Ink)) controls.OpenControls();
        if (Action(new Rect(134, 320, 100, 27), "Privacidad", Border, Ink)) page = Page.Privacy;
        if (Action(new Rect(516, 320, 100, 27), "Salir", Border, Muted)) Quit();
    }

    void DrawHeroArt() {
        Card(new Rect(24, 91, 275, 204), IllustratedHome ? HomeBorder : Border, IllustratedHome ? HomeSurface : Surface);
        Vector2 center = new Vector2(162, 192);
        CoopPromptUI.Ring(center, 81, 1, new Color(.23f, .19f, .34f), 2);
        CoopPromptUI.Ring(center, 67, 1, new Color(.18f, .16f, .28f), 1);
        CoopPromptUI.Line(new Vector2(78, 210), new Vector2(260, 176), 2, Accent);
        CoopPromptUI.Line(new Vector2(78, 214), new Vector2(260, 180), 1, Border);
        CoopPromptUI.Hero(new Rect(48, 106, 132, 150), 0);
        CoopPromptUI.Hero(new Rect(167, 148, 99, 108), 1);
        Text(new Rect(61, 260, 103, 23), "Mimo", 12, Lavender, true, TextAnchor.MiddleCenter);
        Text(new Rect(165, 260, 103, 23), "Pipro", 12, Accent, true, TextAnchor.MiddleCenter);
    }

    bool ModeCard(Rect rect, string title, string detail, Color accent, bool wifi) {
        bool hovered = rect.Contains(Event.current.mousePosition);
        Color surface = IllustratedHome ? HomeSurface : Surface;
        Card(rect, hovered ? accent : IllustratedHome ? HomeBorder : Border,
            hovered ? Color.Lerp(surface, accent, .12f) : surface);
        int mode = wifi ? 1 : 0;
        var pointer = Event.current;
        if (pointer.type == EventType.MouseDown && pointer.button == 0 && rect.Contains(pointer.mousePosition))
            pressedMode = Time.realtimeSinceStartup >= acceptModeInputAt ? mode : -1;
        bool selected = GUI.Button(rect, GUIContent.none, GUIStyle.none) && pressedMode == mode;
        if (pointer.rawType == EventType.MouseUp && pressedMode == mode) pressedMode = -1;
        var at = new Vector2(rect.x + 26, rect.center.y);
        if (wifi) {
            WifiIcon(at, accent);
        } else {
            Card(new Rect(at.x - 13, at.y - 9, 26, 17), accent, Surface);
            CoopPromptUI.Line(at + new Vector2(0, 8), at + new Vector2(0, 13), 1.5f, accent);
            CoopPromptUI.Line(at + new Vector2(-7, 13), at + new Vector2(7, 13), 1.5f, accent);
        }
        Text(new Rect(rect.x + 51, rect.y + 11, 207, 25), title, 17, Ink, true);
        Text(new Rect(rect.x + 51, rect.y + 36, 211, 19), detail, 11, Muted);
        CoopPromptUI.Arrow(new Vector2(rect.xMax - 17, rect.center.y), 3, 6, accent);
        return selected;
    }

    static void WifiIcon(Vector2 center, Color color) {
        var origin = center + new Vector2(0, 9);
        for (int radius = 8; radius <= 18; radius += 5) {
            var previous = origin + new Vector2(Mathf.Cos(-2.3f), Mathf.Sin(-2.3f)) * radius;
            for (int point = 1; point <= 8; point++) {
                float angle = -2.3f + point * .18f;
                var next = origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                CoopPromptUI.Line(previous, next, 1.6f, color);
                previous = next;
            }
        }
        CoopPromptUI.Fill(new Rect(origin.x - 1.5f, origin.y - 1.5f, 3, 3), color);
    }

    void DrawMissions() {
        Text(new Rect(24, 81, 430, 28), "Misiones", 21, Ink, true);
        int unlocked = GameDuoCampaign.UnlockedStage;
        bool hasEnergy = CampaignEnergy.Current.Count > 0;
        for (int i = 0; i < GameDuoCampaign.Scenes.Length; i++) {
            bool locked = i > unlocked;
            bool current = i == unlocked;
            var rect = new Rect(24, 113 + i * 40, 592, 35);
            bool hovered = !locked && hasEnergy && rect.Contains(Event.current.mousePosition);
            Color edge = current || hovered ? Accent : Border;
            Card(rect, edge, hovered ? Color.Lerp(Surface, Accent, .12f) : Surface);
            bool selected = !locked && hasEnergy && GUI.Button(rect, GUIContent.none, GUIStyle.none);
            Text(new Rect(37, rect.y, 32, rect.height), (i + 1).ToString("00"), 12, locked ? Muted : current || hovered ? Accent : Lavender, true);
            Text(new Rect(76, rect.y, 420, rect.height), GameDuoCampaign.Titles[i], 14, locked ? Muted : Ink, !locked);
            Text(new Rect(513, rect.y, 88, rect.height), locked ? "Bloqueada" : hasEnergy ? "Jugar" : "Sin energía", 11,
                locked ? Muted : Accent, !locked, TextAnchor.MiddleRight);
            if (selected) TryStartMission(i);
        }
        Back(new Rect(24, 322, 100, 26));
    }

    void DrawWifi() {
        Text(new Rect(24, 81, 540, 34), "Dos dispositivos", 21, Ink, true);
        var mode = GameDuoLan.Mode;
        if (mode == GameDuoLan.LanMode.Guest) {
            Card(new Rect(174, 128, 292, 86), Border, Surface);
            Text(new Rect(190, 136, 260, 28), "Mando de Pipro", 16, Ink, true, TextAnchor.MiddleCenter);
            CoopPromptUI.Text(new Rect(184, 172, 272, 28), GameDuoLan.Status, 11, Muted);
        } else if (mode == GameDuoLan.LanMode.Host) {
            Card(new Rect(24, 124, 592, 178), Border, Surface);
            Text(new Rect(46, 136, 550, 26), "Anfitrión", 17, Ink, true);
            Text(new Rect(46, 174, 550, 31), "IP para compartir: " + GameDuoLan.Address, 17, Lavender, true);
            CoopPromptUI.Text(new Rect(40, 210, 560, 27), GameDuoLan.Status, 11, Muted);
            if (Action(new Rect(198, 252, 244, 35), CampaignEnergy.Current.Count > 0 ? "Empezar campaña" : "Sin energía", Accent, Ink, GameDuoLan.Connected && CampaignEnergy.Current.Count > 0))
                StartGame();
        } else {
            Text(new Rect(24, 116, 592, 24), "El anfitrión muestra el juego. El otro dispositivo sirve de mando.", 12, Muted);
            Card(new Rect(24, 154, 280, 146), Border, Surface);
            Card(new Rect(320, 154, 296, 146), Border, Surface);
            Text(new Rect(42, 165, 245, 28), "Crear partida", 16, Ink, true);
            Text(new Rect(42, 199, 245, 23), "Empiecen desde la primera misión.", 11, Muted);
            if (Action(new Rect(42, 252, 244, 34), "Crear partida", Accent, Ink)) GameDuoLan.StartHost();
            Text(new Rect(338, 165, 261, 28), "Unirse a una partida", 16, Ink, true);
            Text(new Rect(338, 194, 261, 19), "IP del anfitrión", 11, Muted);
            joinIp = GUI.TextField(new Rect(338, 215, 260, 27), joinIp, 32);
            if (Action(new Rect(338, 252, 260, 34), "Unirme", Lavender, Ink, !string.IsNullOrWhiteSpace(joinIp)))
                GameDuoLan.Join(joinIp);
        }

        var back = mode == GameDuoLan.LanMode.Guest ? new Rect(270, 236, 100, 28) : new Rect(24, 322, 100, 26);
        if (Action(back, "Volver", Border, Ink)) {
            GameDuoLan.Local();
            if (mode == GameDuoLan.LanMode.Local) page = Page.Home;
        }
    }

    void DrawPrivacy() {
        Text(new Rect(24, 81, 592, 34), "Privacidad de Siempre Juntos", 21, Ink, true);
        Card(new Rect(24, 126, 592, 157), Border, Surface);
        CoopPromptUI.Text(new Rect(45, 135, 550, 130),
            "Tu progreso se guarda en este dispositivo. Siempre Juntos no requiere una cuenta. Puedes elegir ver un anuncio de Google AdMob para recuperar energía.\n\n" +
            "La IP y los controles se usan solo en tu red Wi-Fi. No se envían a servidores externos ni a los desarrolladores.", 13, Muted);
        Back(new Rect(24, 322, 100, 26));
    }

    void Back(Rect rect) {
        if (Action(rect, "Volver", Border, Ink)) page = Page.Home;
    }

    bool Action(Rect rect, string title, Color accent, Color color, bool enabled = true) {
        bool hovered = enabled && rect.Contains(Event.current.mousePosition);
        Color surface = IllustratedHome ? HomeSurface : Surface;
        Color edge = IllustratedHome && accent == Border ? HomeBorder : accent;
        Card(rect, enabled ? edge : Border, hovered ? Color.Lerp(surface, edge, .17f) : surface);
        bool selected = enabled && GUI.Button(rect, GUIContent.none, GUIStyle.none);
        Text(rect, title, 12, enabled ? color : Muted, true, TextAnchor.MiddleCenter);
        return selected;
    }

    static void Card(Rect rect, Color border, Color fill) {
        CoopPromptUI.Fill(rect, fill);
        CoopPromptUI.Fill(new Rect(rect.x, rect.y, rect.width, 1), border);
        CoopPromptUI.Fill(new Rect(rect.x, rect.yMax - 1, rect.width, 1), border);
        CoopPromptUI.Fill(new Rect(rect.x, rect.y + 1, 1, rect.height - 2), border);
        CoopPromptUI.Fill(new Rect(rect.xMax - 1, rect.y + 1, 1, rect.height - 2), border);
    }

    void Text(Rect rect, string value, int size, Color color, bool bold = false, TextAnchor align = TextAnchor.MiddleLeft) {
        if (label == null) label = new GUIStyle(GUI.skin.label) { padding = new RectOffset() };
        label.fontSize = size;
        label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        label.alignment = align;
        label.normal.textColor = color;
        GUI.Label(rect, value, label);
    }

    static void Quit() {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
}
