using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TABCamera.Plugin.Patches
{
    [HarmonyPatch(typeof(UpdateFreeCamera))]
    internal static class UpdateFreeCameraPatch
    {
        private static float Speed = 1.6f;
        private static float SpeedMultiplier = 4f;
        private static float Sensitivity = 0.07f;

        private static float MinFov = 2f;
        private static float MaxFov = 170f;

        private static float FovSensitivity = 0.35f;

        private static bool _fpsMode = true;

        private static Vector3 _oldCamPos;
        private static Quaternion _oldCamRot;

        private static Vector3 _oldLookPos;
        private static Quaternion _oldLookRot;

        private static bool _wasPhotoMode = false;

        private static bool _forced = false;

        public static bool IsFPSMode(Player player)
        {
            return _fpsMode && (player.State == GameStates.Photo || _forced);
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(UpdateFreeCamera.UpdateForPlayer))]
        private static bool UpdateForPlayer_Prefix(Player player, UpdateFreeCamera __instance)
        {
            if (Keyboard.current.f8Key.wasPressedThisFrame) _forced = !_forced;

            var hybridPlayer = PlayerManager.Instance.GetHybridPlayer(player.PlayerIndex);
            var camTf = hybridPlayer.HybridCamera.FreeCamera.CameraTransform;
            var lookTf = hybridPlayer.HybridCamera.FreeCamera.LookTarget;

            if (player.State == GameStates.Photo)
                _forced = false;

            if (player.State != GameStates.Photo && !_forced)
            {
                if (_wasPhotoMode)
                {
                    camTf.position = _oldCamPos;
                    camTf.rotation = _oldCamRot;
                    lookTf.position = _oldLookPos;
                    lookTf.rotation = _oldLookRot;
                    _wasPhotoMode = false;
                }
                _oldCamPos = camTf.position;
                _oldCamRot = camTf.rotation;

                _oldLookPos = lookTf.position;
                _oldLookRot = lookTf.rotation;
                _fpsMode = true;
                return true;
            }

            _wasPhotoMode = true;

            if (Keyboard.current.leftAltKey.wasPressedThisFrame) _fpsMode = !_fpsMode;

            if (!_fpsMode)
            {
                CursorManager.Instance.MouseLockedInPlace = false;
            }
            else
            {
                CursorManager.Instance.MouseLockedInPlace = true;
            }

            var input2d = new Vector2((Keyboard.current.dKey.isPressed ? 1f : 0f) - (Keyboard.current.aKey.isPressed ? 1f : 0f), (Keyboard.current.wKey.isPressed ? 1f : 0f) - (Keyboard.current.sKey.isPressed ? 1f : 0f));

            var currentSpeed = Speed;

            if (Keyboard.current.leftShiftKey.isPressed) currentSpeed *= SpeedMultiplier;

            var flatForward = camTf.forward;
            flatForward.y = 0f;
            flatForward = flatForward.normalized;

            var flatRight = camTf.right;
            flatRight.y = 0f;
            flatRight = flatRight.normalized;

            lookTf.position += flatForward * input2d.y * currentSpeed * Time.unscaledDeltaTime;
            lookTf.position += flatRight * input2d.x * currentSpeed * Time.unscaledDeltaTime;

            var heightInput = (Keyboard.current.eKey.isPressed ? 1f : 0f) - (Keyboard.current.qKey.isPressed ? 1f : 0f);

            lookTf.position += Vector3.up * heightInput * currentSpeed * Time.unscaledDeltaTime;

            if (_fpsMode)
            {
                var delta = Mouse.current.delta.ReadValue();

                if (Mouse.current.rightButton.isPressed)
                {
                    var fov = hybridPlayer.HybridCamera.FreeCamera.CurrentFieldOfView;

                    fov -= delta.y * FovSensitivity;

                    fov = Mathf.Clamp(fov, MinFov, MaxFov);

                    hybridPlayer.HybridCamera.FreeCamera.CurrentFieldOfView = fov;
                }
                else
                {
                    camTf.Rotate(Vector3.up * delta.x * Sensitivity, Space.World);
                    camTf.Rotate(Vector3.right * -delta.y * Sensitivity, Space.Self);
                }
            }

            camTf.position = lookTf.position;

            return false;
        }
    }
}
