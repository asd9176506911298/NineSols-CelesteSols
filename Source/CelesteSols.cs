using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using NineSolsAPI;
using UnityEngine;

namespace CelesteSols;

[BepInDependency(NineSolsAPICore.PluginGUID)]
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class CelesteSols : BaseUnityPlugin {
    private ConfigEntry<bool> enableSomethingConfig = null!;
    private ConfigEntry<KeyboardShortcut> somethingKeyboardShortcut = null!;

    public static bool isFallEnable = false;
    private float dashSpeed = 350f;
    Vector2 dashVelocity;
    private AnimationCurve dashSpeedCurve = new AnimationCurve(
    new Keyframe(0f, 1f, 0f, 0f),         // 開始瞬間最高速
    new Keyframe(0.8f, 1f, 0f, 0f),       // 中段保持
    new Keyframe(1f, 0f, -20f, 0f)        // 結尾快速減速
);
    private Vector2 currentDashDirection = Vector2.zero;
    private Vector2 lastDashDir = Vector2.zero;
    private bool queuedBoost = false;

    private bool canDash = true;
    private bool isDashing = false;
    private float dashTimeElapsed = 0f;
    private float dashMaxDuration = 0.3f; // 確保不會一直 dash（防止卡住）

    private float lastDashEndTime = -999f;
    private float dashBoostMemoryDuration = 0.2f; // 可容忍 boost 的最大時間
    private Vector2 lastDashDirection = Vector2.zero;


    private Harmony harmony = null!;

    private void Awake() {
        Log.Init(Logger);
        RCGLifeCycle.DontDestroyForever(gameObject);

        // Load patches from any class annotated with @HarmonyPatch
        harmony = Harmony.CreateAndPatchAll(typeof(CelesteSols).Assembly);

        enableSomethingConfig = Config.Bind("General.Something", "Enable", true, "Enable the thing");
        somethingKeyboardShortcut = Config.Bind("General.Something", "Shortcut",
            new KeyboardShortcut(KeyCode.H, KeyCode.LeftControl), "Shortcut to execute");

        KeybindManager.Add(this, TestMethod, () => somethingKeyboardShortcut.Value);

        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }

    void StartDash(Vector2 velocity) {
        isDashing = true;
        dashTimeElapsed = 0f;
        dashVelocity = velocity;
        currentDashDirection = velocity.normalized;
        isFallEnable = false;
    }

    void EndDash() {
        isDashing = false;
        isFallEnable = true;
        canDash = true; 

        lastDashEndTime = Time.time;
        lastDashDirection = currentDashDirection.normalized;
    }

    void Jump() {
        float sinceDash = Time.time - lastDashEndTime;

        bool recentDash = sinceDash <= dashBoostMemoryDuration;
        bool downAndSide = lastDashDirection.y < -0.1f && Mathf.Abs(lastDashDirection.x) > 0.1f;

        if (recentDash && downAndSide) {
            float xBoost = lastDashDirection.x * 300f;
            float yBoost = 420f;
            Player.i.Velocity += new Vector2(xBoost, yBoost);
            Logger.LogInfo($"[Boost] Applied! dir={lastDashDirection}");
        }
    }

    private void FixedUpdate() {
        if (Player.i.onGround) {
            canDash = true;
        }

        if (canDash && Input.GetKeyDown(KeyCode.L)) {
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

            if (t >= 1f) {
                EndDash();
            }
        }

        if (Input.GetKeyDown(KeyCode.Space)) {
            if (isDashing) {
                EndDash(); // 提前結束 dash
            }

            Jump(); // boost 包在裡面
        }
    }



    private Vector2 GetDashDirection() {
        bool up = Input.GetKey(KeyCode.W);
        bool down = Input.GetKey(KeyCode.S);
        bool left = Input.GetKey(KeyCode.A);
        bool right = Input.GetKey(KeyCode.D);

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
        if (!enableSomethingConfig.Value) return;
        ToastManager.Toast("Shortcut activated");
        Log.Info("Log messages will only show up in the logging console and LogOutput.txt");
    }

    private void OnDestroy() {
        // Make sure to clean up resources here to support hot reloading

        harmony.UnpatchSelf();
    }
}