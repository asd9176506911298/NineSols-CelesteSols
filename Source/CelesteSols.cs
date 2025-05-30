using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using I2.Loc;
using NineSolsAPI;
using UnityEngine;

namespace CelesteSols;

[BepInDependency(NineSolsAPICore.PluginGUID)]
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class CelesteSols : BaseUnityPlugin {
    private ConfigEntry<bool> unlimitedDashEnabled = null!;
    private ConfigEntry<KeyboardShortcut> somethingKeyboardShortcut = null!;
    private ConfigEntry<KeyCode> dashKey = null!;

    public static bool isFallEnable = false;

    private Harmony harmony = null!;

    // Dash parameters
    private float dashSpeed = 350f;
    private float dashMaxDuration = 0.3f;
    private float dashTimeElapsed = 0f;
    private Vector2 dashVelocity;
    private Vector2 currentDashDirection = Vector2.zero;
    private bool isDashing = false;
    private bool canDash = true;

    // Boost parameters
    private float lastDashEndTime = -999f;
    private float dashBoostMemoryDuration = 0.2f;
    private Vector2 lastDashDirection = Vector2.zero;
    private Vector2 lastDashVelocity = Vector2.zero;
    [SerializeField] private float boostStrengthMultiplier = 1.2f;
    [SerializeField] private float boostVerticalMinimum = 300f;

    private AnimationCurve dashSpeedCurve = new(
        new Keyframe(0f, 1f, 0f, 0f),
        new Keyframe(0.8f, 1f, 0f, 0f),
        new Keyframe(1f, 0f, -20f, 0f)
    );

    private void Awake() {
        Log.Init(Logger);
        RCGLifeCycle.DontDestroyForever(gameObject);

        harmony = Harmony.CreateAndPatchAll(typeof(CelesteSols).Assembly);

        unlimitedDashEnabled = Config.Bind("Dash", "UnlimitedDash", false, "Allow unlimited dash without needing to touch ground");
        dashKey = Config.Bind("Dash", "DashKey", KeyCode.X, "");

        KeybindManager.Add(this, TestMethod, () => new KeyboardShortcut(KeyCode.H, KeyCode.LeftControl));

        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }

    private void FixedUpdate() {
        if (Player.i.onGround || unlimitedDashEnabled.Value) canDash = true;

        if (canDash && Input.GetKeyDown(dashKey.Value)) {
            Vector2 dir = GetDashDirection();
            if (dir != Vector2.zero) {
                StartDash(dir.normalized * dashSpeed);
                canDash = false;
            }
        }

        if (isDashing) {
            dashTimeElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(dashTimeElapsed / dashMaxDuration);
            float speedMultiplier = dashSpeedCurve.Evaluate(t);
            Player.i.Velocity = dashVelocity * speedMultiplier;
            if (t >= 1f) EndDash();
        }

        if (Player.i.playerInput.gameplayActions.Jump.IsPressed && !Player.i.CanJump && Player.i.onGround) {
            if (isDashing) EndDash();
            Jump();
        }
    }

    private void StartDash(Vector2 velocity) {
        isDashing = true;
        dashTimeElapsed = 0f;
        dashVelocity = velocity;
        currentDashDirection = velocity.normalized;
        isFallEnable = false;
    }

    private void EndDash() {
        isDashing = false;
        isFallEnable = true;

        lastDashEndTime = Time.time;
        lastDashDirection = currentDashDirection.normalized;
        lastDashVelocity = dashVelocity;
    }

    private void Jump() {
        float sinceDash = Time.time - lastDashEndTime;
        bool recentDash = sinceDash <= dashBoostMemoryDuration;
        bool downAndSide = lastDashDirection.y < -0.1f && Mathf.Abs(lastDashDirection.x) > 0.1f;

        if (recentDash && downAndSide) {
            Vector2 boost = lastDashDirection.normalized * lastDashVelocity.magnitude * boostStrengthMultiplier;
            if (boost.y < boostVerticalMinimum) boost.y = boostVerticalMinimum;
            Player.i.Velocity += boost;
            Logger.LogInfo($"[Boost] Applied! dir={lastDashDirection}, mag={lastDashVelocity.magnitude}, boost={boost}");
        }
    }

    private Vector2 GetDashDirection() {
        bool up = Player.i.playerInput.gameplayActions.MoveUp.IsPressed;
        bool down = Player.i.playerInput.gameplayActions.MoveDown.IsPressed;
        bool left = Player.i.playerInput.gameplayActions.MoveLeft.IsPressed;
        bool right = Player.i.playerInput.gameplayActions.MoveRight.IsPressed;

        bool onGround = Player.i.onGround;

        if (onGround) {
            if ((down && left) || (down && right)) return Vector2.zero;
        }

        if (up && left) return new Vector2(-1, 1);
        if (up && right) return new Vector2(1, 1);
        if (up) return new Vector2(0, 1);
        if (down && left) return new Vector2(-1, -1);
        if (down && right) return new Vector2(1, -1);
        if (down) return new Vector2(0, -1);
        if (left) return new Vector2(-1, 0);
        if (right) return new Vector2(1, 0);

        return Vector2.zero;
    }

    private void TestMethod() {
        ToastManager.Toast("Create GoodtimeEigong");
        var x = Instantiate(GameObject.Find("GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/UI-Canvas/[Tab] MenuTab/CursorProvider/Menu Vertical Layout/Panels/Boss Challenge Panel/MainMapPanel(Mask1)/ScaleRoot/Scroll View/Viewport/(Shared Content For WorldMap And Teleport Map)Content/Teleport Button_A2_S5"));
        AutoAttributeManager.AutoReference(x);
        AutoAttributeManager.AutoReferenceAllChildren(x);

        x.name = "goodtimeEigong";
        x.transform.position = GameObject.Find("GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/UI-Canvas/[Tab] MenuTab/CursorProvider/Menu Vertical Layout/Panels/Boss Challenge Panel/MainMapPanel(Mask1)/ScaleRoot/Scroll View/Viewport/(Shared Content For WorldMap And Teleport Map)Content/Teleport Button_A2_S5").transform.position;
        //x.transform.localPosition = GameObject.Find("GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/UI-Canvas/[Tab] MenuTab/CursorProvider/Menu Vertical Layout/Panels/Boss Challenge Panel/MainMapPanel(Mask1)/ScaleRoot/Scroll View/Viewport/(Shared Content For WorldMap And Teleport Map)Content/Teleport Button_A2_S5").transform.localPosition;
        x.transform.parent = GameObject.Find("GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/UI-Canvas/[Tab] MenuTab/CursorProvider/Menu Vertical Layout/Panels/Boss Challenge Panel/MainMapPanel(Mask1)/ScaleRoot/Scroll View/Viewport/(Shared Content For WorldMap And Teleport Map)Content").transform;
        x.transform.localPosition -= new Vector3(150f, 0f, 0f);
        //x.GetComponent<TeleportPointButton>().teleportPoint.levelMapData.name = "goodtimeEigong";
        x.GetComponent<TeleportPointButton>().teleportPoint.titleStr.mTerm = "";
        x.GetComponent<TeleportPointButton>().teleportPoint.titleStr.placeHolder = "goodtimeEigong";
        x.GetComponent<TeleportPointButton>().OnSelectedEvent.AddListener((button) => ToastManager.Toast("I'm GoodtimeEigong"));

        GameObject.Find("GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/UI-Canvas/[Tab] MenuTab/CursorProvider/Menu Vertical Layout/Panels/Boss Challenge Panel").GetComponent<TeleportUIPanelController>().teleportPointButtons.Add(x.GetComponent<TeleportPointButton>());
    }

    private void OnDestroy() {
        harmony.UnpatchSelf();
    }
}
