using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using NineSolsAPI;
using System;
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

    // Dash state
    private float dashSpeed = 360f;                // 原始 dash 速度提高
    private float dashDuration = 0.15f;
    private float dashTimeElapsed = 0f;
    private bool isDashing = false;
    private Vector2 dashVelocity;

    // Dash count
    private int maxDashes = 1;
    private int currentDashes = 1;

    // Boost logic
    private float lastDashEndTime = -999f;
    private Vector2 lastDashDirection = Vector2.zero;
    private Vector2 lastDashVelocity = Vector2.zero;
    private float dashBoostMemoryDuration = 0.2f;
    private float boostStrengthMultiplier = 2.1f;
    private float boostVerticalMinimum = 300f;

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
        // 重設 Dash（觸地 or 無限）
        if (Player.i.onGround || unlimitedDashEnabled.Value)
            currentDashes = maxDashes;

        // 啟動 Dash
        if (!isDashing && currentDashes > 0 && Input.GetKeyDown(dashKey.Value)) {
            Vector2 dir = GetDashDirection();
            if (dir != Vector2.zero) {
                StartDash(dir.normalized);
            }
        }

        // Dash 持續中
        if (isDashing) {
            dashTimeElapsed += Time.deltaTime;
            Player.i.Velocity = dashVelocity;

            if (dashTimeElapsed >= dashDuration) {
                EndDash();
            }
        }

        // Dash-Jump Boost
        if (Player.i.playerInput.gameplayActions.Jump.IsPressed && !Player.i.CanJump && Player.i.onGround) {
            if (isDashing) EndDash();
            JumpWithBoost();
        }
    }

    private void StartDash(Vector2 dir) {
        isDashing = true;
        dashTimeElapsed = 0f;
        dashVelocity = dir * dashSpeed;
        currentDashes--;

        lastDashDirection = dir;
        lastDashVelocity = dashVelocity;
        lastDashEndTime = -999f;
        isFallEnable = false;
    }

    private void EndDash() {
        isDashing = false;
        isFallEnable = true;
        lastDashEndTime = Time.time;
    }

    private void JumpWithBoost() {
        float sinceDash = Time.time - lastDashEndTime;
        bool canBoost = sinceDash <= dashBoostMemoryDuration;
        bool downAndSide = lastDashDirection.y < -0.1f && Mathf.Abs(lastDashDirection.x) > 0.1f;

        if (canBoost && downAndSide) {
            Vector2 boost = lastDashDirection.normalized * lastDashVelocity.magnitude * boostStrengthMultiplier;

            boost.y = Math.Abs(boost.y) / 2;

            Player.i.Velocity = boost;
            Logger.LogInfo($"[Boost Jump] boost={boost}");
        } else {
            Player.i.Velocity = new Vector2(Player.i.Velocity.x, -300f);
        }
    }


    private Vector2 GetDashDirection() {
        bool up = Player.i.playerInput.gameplayActions.MoveUp.IsPressed;
        bool down = Player.i.playerInput.gameplayActions.MoveDown.IsPressed;
        bool left = Player.i.playerInput.gameplayActions.MoveLeft.IsPressed;
        bool right = Player.i.playerInput.gameplayActions.MoveRight.IsPressed;

        bool onGround = Player.i.onGround;
        if (onGround && ((down && left) || (down && right)))
            return Vector2.zero;

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
        ToastManager.Toast("Shortcut activated");
        Log.Info("Log messages will only show up in the logging console and LogOutput.txt");
    }

    private void OnDestroy() {
        harmony.UnpatchSelf();
    }
}
