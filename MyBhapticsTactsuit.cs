using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Resources;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MelonLoader;
using Bhaptics.SDK2;
using System.Security.Cryptography;

namespace MyBhapticsTactsuit
{
    public class TactsuitVR
    {
        public bool suitDisabled = true;
        public bool systemInitialized = false;
        private static ManualResetEvent HeartBeat_mrse = new ManualResetEvent(false);
        public Dictionary<String, String> FeedbackMap = new Dictionary<String, String>();

        public void HeartBeatFunc()
        {
            while (true)
            {
                HeartBeat_mrse.WaitOne();

                BhapticsSDK2.Play("HeartBeat".ToLower());
                Thread.Sleep(1000);
            }
        }

        public TactsuitVR()
        {
            LOG("Initializing suit");
            // TODO need to check if this works
            string configPath = Directory.GetCurrentDirectory() + "\\Mods\\bhaptics_library.dev.dll";
            NativeLibrary.Load(configPath);
            var res = BhapticsSDK2.Initialize("EbZ73nerOmcM3AOVoyr2", "Df9MuZU0Q9x2VEh27MwU");

            if (res != 0)
            {
                suitDisabled = false;
            }
            //RegisterAllTactFiles();
            LOG("Starting HeartBeat and NeckTingle thread...");
            Thread HeartBeatThread = new Thread(HeartBeatFunc);
            HeartBeatThread.Start();
        }

        public void LOG(string logStr)
        {
            MelonLogger.Msg(logStr);
        }

        public void PlaybackHaptics(String key, float intensity = 1.0f, float duration = 1.0f)
        {
            BhapticsSDK2.Play(key.ToLower(), intensity, duration, 0f, 0f);
            // LOG("Playing back: " + key);
            
        }

        public void PlayBackHit(String key, float xzAngle, float yShift)
        {
            // two parameters can be given to the pattern to move it on the vest:
            // 1. An angle in degrees [0, 360] to turn the pattern to the left
            // 2. A shift [-0.5, 0.5] in y-direction (up and down) to move it up or down
            BhapticsSDK2.Play(key.ToLower(), 1f, 1f, xzAngle, yShift);
        }

        public void GunRecoil(bool isRightHand, float intensity = 1.0f, bool twoHanded = false )
        {
            float duration = 1.0f;
            string postfix = "_L";
            string otherPostfix = "_R";
            if (isRightHand) { postfix = "_R"; otherPostfix = "_L"; }
            string keyArm = "RecoilArms" + postfix;
            string keyVest = "RecoilVest" + postfix;
            string keyHands = "RecoilHands" + postfix;
            string keyArmOther = "RecoilArms" + otherPostfix;
            string keyHandsOther = "RecoilHands" + otherPostfix;
            BhapticsSDK2.Play(keyHands.ToLower(), intensity, duration, 0f, 0f);
            BhapticsSDK2.Play(keyArm.ToLower(), intensity, duration, 0f, 0f);
            BhapticsSDK2.Play(keyVest.ToLower(), intensity, duration, 0f, 0f);

            if (twoHanded)
            {
                BhapticsSDK2.Play(keyHandsOther.ToLower(), intensity, duration, 0f, 0f);
                BhapticsSDK2.Play(keyArmOther.ToLower(), intensity, duration, 0f, 0f);
            }
        }

        public void CastSpell(bool isRightHand, float intensity = 1.0f)
        {
            float duration = 1.0f;
            string postfix = "_L";
            if (isRightHand) { postfix = "_R"; }

            string keyHand = "SpellHand" + postfix;
            string keyArm = "SpellArm" + postfix;
            string keyVest = "SpellVest" + postfix;

            BhapticsSDK2.Play(keyHand.ToLower(), intensity, duration, 0f, 0f);
            BhapticsSDK2.Play(keyArm.ToLower(), intensity, duration, 0f, 0f);
            BhapticsSDK2.Play(keyVest.ToLower(), intensity, duration, 0f, 0f);
        }

        public void ShootBow(bool isRightHand, float intensity = 1.0f)
        {
            float duration = 1.0f;
            string postfix = "_L";
            if (isRightHand) { postfix = "_R"; }

            string keyVest = "ShootBowVest" + postfix;

            BhapticsSDK2.Play(keyVest.ToLower(), intensity, duration, 0f, 0f);
        }


        public void SwordRecoil(bool isRightHand, float intensity = 1.0f)
        {
            float duration = 1.0f;
            string postfix = "_L";
            if (isRightHand) { postfix = "_R"; }
            string keyArm = "SwordArms" + postfix;
            string keyVest = "SwordVest" + postfix;
            string keyHands = "RecoilHands" + postfix;

            BhapticsSDK2.Play(keyHands.ToLower(), intensity, duration, 0f, 0f);
            BhapticsSDK2.Play(keyArm.ToLower(), intensity, duration, 0f, 0f);
            BhapticsSDK2.Play(keyVest.ToLower(), intensity, duration, 0f, 0f);
        }

        public void HeadShot(float hitAngle)
        {
            
            if (IsDeviceConnected(PositionType.Head))
            {
                if ((hitAngle < 45f) | (hitAngle > 315f)) { PlaybackHaptics("Headshot_F"); }
                if ((hitAngle > 45f) && (hitAngle < 135f)) { PlaybackHaptics("Headshot_L"); }
                if ((hitAngle > 135f) && (hitAngle < 225f)) { PlaybackHaptics("Headshot_B"); }
                if ((hitAngle > 225f) && (hitAngle < 315f)) { PlaybackHaptics("Headshot_R"); }
            }
            else { PlayBackHit("BulletHit", hitAngle, 0.5f); }
        }

        private bool IsDeviceConnected(PositionType pos) 
        {
            var devices = BhapticsSDK2.GetDevices();

            for (int i = 0; i < devices.Count; i++) {
                if (devices[i].IsConnected && devices[i].Position == pos) {
                    return true;
                }
            }

            return false;
        }

        public void FootStep(bool isRightFoot)
        {
            if (!IsDeviceConnected(PositionType.FootL)) { return; }
            string postfix = "_L";
            if (isRightFoot) { postfix = "_R"; }
            string key = "FootStep" + postfix;
            PlaybackHaptics(key);
        }

        public void StartHeartBeat()
        {
            HeartBeat_mrse.Set();
        }

        public void StopHeartBeat()
        {
            HeartBeat_mrse.Reset();
        }

        public bool IsPlaying(String effect)
        {
            return BhapticsSDK2.IsPlaying(effect.ToLower());
        }

        public void StopHapticFeedback(String effect)
        {
            BhapticsSDK2.Stop(effect.ToLower());
        }

        public void StopAllHapticFeedback()
        {
            StopThreads();

            BhapticsSDK2.StopAll();
        }

        public void StopThreads()
        {
            StopHeartBeat();
        }


    }
}
