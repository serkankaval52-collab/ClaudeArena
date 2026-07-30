using UnityEngine;

namespace CoreFactory.Utils
{
    public enum HapticEvent { Selection, LightImpact, SoftImpact, MediumImpact, RigidImpact, HeavyImpact, Success, Warning, Failure }

    public static class HapticFeedbackHelper
    {
        private static bool _hapticsDisabled;
        private static bool _permissionWarningShown;

        public static bool Enabled { get; set; } = true;

        public static void Play(HapticEvent hapticEvent)
        {
            if (!Enabled || _hapticsDisabled) return;
            int iosStyle = MapEventToIosStyle(hapticEvent);
            float androidAmp = MapEventToAndroidAmplitude(hapticEvent);
            int androidDuration = MapEventToAndroidDuration(hapticEvent);
            Dispatch(iosStyle, androidAmp, androidDuration);
        }

        public static void TriggerHapticPulse(float amplitude, int durationMs)
        {
            if (!Enabled || _hapticsDisabled) return;
            if (amplitude <= 0.001f || durationMs <= 0) return;

            amplitude = Mathf.Clamp01(amplitude);
            int iosStyle = amplitude <= 0.33f ? 0 : (amplitude <= 0.66f ? 1 : 2);
            Dispatch(iosStyle, amplitude, durationMs);
        }

        private static void Dispatch(int iosStyle, float androidAmplitude, int durationMs)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            PlayAndroid(androidAmplitude, durationMs);
#elif UNITY_IOS && !UNITY_EDITOR
            PlayIos(iosStyle);
#else
            Debug.Log($"[Haptic Mock] Style: {iosStyle}, Amp: {androidAmplitude:F2}, Dur: {durationMs}ms");
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject _vibrator;
        private static bool _androidProbed;
        private static int _sdkInt;

        private static void PlayAndroid(float amplitude, int durationMs)
        {
            try
            {
                EnsureAndroidVibrator();
                if (_vibrator == null) return;

                int intAmplitude = Mathf.Clamp(Mathf.RoundToInt(amplitude * 255f), 1, 255);
                if (_sdkInt >= 26)
                {
                    using (var effectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                    using (var effect = effectClass.CallStatic<AndroidJavaObject>("createOneShot", (long)durationMs, intAmplitude))
                    {
                        _vibrator.Call("vibrate", effect);
                    }
                }
                else
                {
                    _vibrator.Call("vibrate", (long)durationMs);
                }
            }
            catch (AndroidJavaException e)
            {
                if (!_permissionWarningShown)
                {
                    _permissionWarningShown = true;
                    Debug.LogError("[Haptic] Android Exception: Missing VIBRATE permission in Manifest.");
                }
                _hapticsDisabled = true;
            }
            catch (System.Exception)
            {
                _hapticsDisabled = true;
            }
        }

        private static void EnsureAndroidVibrator()
        {
            if (_androidProbed) return;
            _androidProbed = true;

            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                _sdkInt = version.GetStatic<int>("SDK_INT");
            }

            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                if (_sdkInt >= 31)
                {
                    using (var manager = activity.Call<AndroidJavaObject>("getSystemService", "vibrator_manager"))
                    {
                        _vibrator = manager?.Call<AndroidJavaObject>("getDefaultVibrator");
                    }
                }
                else
                {
                    _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }
            }

            if (_vibrator != null && !_vibrator.Call<bool>("hasVibrator"))
            {
                _vibrator = null;
                _hapticsDisabled = true;
            }
        }
#endif

#if UNITY_IOS && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void _PlayiOSHapticImpact(int style);
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void _PrepareiOSHaptic(int style);

        private static void PlayIos(int style) => _PlayiOSHapticImpact(style);
        public static void Prepare(HapticEvent hapticEvent) => _PrepareiOSHaptic(MapEventToIosStyle(hapticEvent));
#else
        public static void Prepare(HapticEvent hapticEvent) { }
#endif

        private static int MapEventToIosStyle(HapticEvent e) => e switch
        {
            HapticEvent.Selection => 0,
            HapticEvent.LightImpact => 0,
            HapticEvent.SoftImpact => 3,
            HapticEvent.MediumImpact => 1,
            HapticEvent.RigidImpact => 4,
            HapticEvent.HeavyImpact => 2,
            HapticEvent.Success => 1,
            HapticEvent.Warning => 4,
            HapticEvent.Failure => 2,
            _ => 1
        };

        private static float MapEventToAndroidAmplitude(HapticEvent e) => e switch
        {
            HapticEvent.Selection => 0.25f,
            HapticEvent.LightImpact => 0.30f,
            HapticEvent.SoftImpact => 0.40f,
            HapticEvent.MediumImpact => 0.60f,
            HapticEvent.RigidImpact => 0.75f,
            HapticEvent.HeavyImpact => 1.00f,
            HapticEvent.Success => 0.55f,
            HapticEvent.Warning => 0.70f,
            HapticEvent.Failure => 0.95f,
            _ => 0.60f
        };

        private static int MapEventToAndroidDuration(HapticEvent e) => e switch
        {
            HapticEvent.Selection => 10,
            HapticEvent.LightImpact => 15,
            HapticEvent.SoftImpact => 25,
            HapticEvent.MediumImpact => 30,
            HapticEvent.RigidImpact => 25,
            HapticEvent.HeavyImpact => 50,
            HapticEvent.Success => 30,
            HapticEvent.Warning => 40,
            HapticEvent.Failure => 60,
            _ => 30
        };
    }
}