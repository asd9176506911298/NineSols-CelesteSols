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
    private float dashSpeed = 500f;
    private float dashDuration = 0.2f;
    Vector2 dashVelocity;
    float dashTimer = 0f;
    private AnimationCurve dashSpeedCurve = new AnimationCurve(
    new Keyframe(0f, 1f, 0f, 0f),         // 開始瞬間最高速
    new Keyframe(0.8f, 1f, 0f, 0f),       // 中段保持
    new Keyframe(1f, 0f, -20f, 0f)        // 結尾快速減速
);
    private float dashTimeElapsed = 0f;

    bool isDashing = false;

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

    void StartDash(Vector2 dashVelocity) {
        isDashing = true;
        dashTimer = dashDuration;
        dashTimeElapsed = 0f;

        isFallEnable = false;

        this.dashVelocity = dashVelocity;
    }

    void EndDash() {
        isDashing = false;

        isFallEnable = true;
    }

    private void FixedUpdate() {
        if (!isDashing && Input.GetKeyDown(KeyCode.L)) {
            Vector2 dir = GetDashDirection();
            if (dir != Vector2.zero) {
                StartDash(dir.normalized * dashSpeed);
            }
        }

        if (isDashing) {
            dashTimer -= Time.deltaTime;
            dashTimeElapsed += Time.deltaTime;

            float t = Mathf.Clamp01(dashTimeElapsed / dashDuration);
            float speedMultiplier = dashSpeedCurve.Evaluate(t);
            Player.i.Velocity = dashVelocity * speedMultiplier;

            if (dashTimer <= 0f) {
                EndDash();
            }
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