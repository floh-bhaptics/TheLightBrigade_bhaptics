using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MelonLoader;
using HarmonyLib;
using MyBhapticsTactsuit;
using LB;
using UnityEngine;

[assembly: MelonInfo(typeof(TheLightBrigade_bhaptics.TheLightBrigade_bhaptics), "TheLightBrigade_bhaptics", "1.0.0", "Florian Fahrenberger")]
[assembly: MelonGame("Funktronic Labs", "The Light Brigade")]


namespace TheLightBrigade_bhaptics
{
    public class TheLightBrigade_bhaptics: MelonMod
    {
        public static TactsuitVR tactsuitVr;

        public override void OnInitializeMelon()
        {
            tactsuitVr = new TactsuitVR();
            tactsuitVr.PlaybackHaptics("HeartBeat");
        }

        [HarmonyPatch(typeof(PlayerActor), "DoDeath", new Type[] { typeof(DeathEntry) })]
        public class bhaptics_PlayerDies
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.StopThreads();
            }
        }

        [HarmonyPatch(typeof(JuiceVolume), "FadeIn", new Type[] { typeof(JuiceVolume.JuiceLayerName), typeof(float), typeof(float) })]
        public class bhaptics_LowHealthVignetteOn
        {
            [HarmonyPostfix]
            public static void Postfix(JuiceVolume.JuiceLayerName layerName)
            {
                if (layerName == JuiceVolume.JuiceLayerName.LowHealth) tactsuitVr.StartHeartBeat();
            }
        }

        [HarmonyPatch(typeof(JuiceVolume), "FadeOut", new Type[] { typeof(JuiceVolume.JuiceLayerName), typeof(float), typeof(float) })]
        public class bhaptics_LowHealthVignetteOff
        {
            [HarmonyPostfix]
            public static void Postfix(JuiceVolume.JuiceLayerName layerName)
            {
                if (layerName == JuiceVolume.JuiceLayerName.LowHealth) tactsuitVr.StopHeartBeat();
            }
        }

        [HarmonyPatch(typeof(JuiceVolume), "FadeOutAll", new Type[] { typeof(float) })]
        public class bhaptics_AllVignetteOff
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.StopThreads();
            }
        }

        [HarmonyPatch(typeof(Weapon_Rifle), "TryFire", new Type[] {  })]
        public class bhaptics_RifleFire
        {
            [HarmonyPostfix]
            public static void Postfix(Weapon_Rifle __instance, bool ___boltOpenState, float ___nextShot, bool ___hammerOpenState)
            {
                if (
                    (___boltOpenState) ||
                    ((double)Time.time < (double)___nextShot) ||
                    ((UnityEngine.Object)__instance.nodeHammer != (UnityEngine.Object)null && ___hammerOpenState) ||
                    ((BaseConfig)__instance.chamber == (BaseConfig)null || __instance.chamberSpent)
                    ) return;
                bool isRight = __instance.grabTrigger.gripController.IsRightController();
                bool twoHanded = false;
                if ((UnityEngine.Object)__instance.grabBarrel != (UnityEngine.Object)null) twoHanded = true;
                //bool twoHanded = __instance.grabTrigger.alternateGrabAlso;
                tactsuitVr.GunRecoil(isRight, 1.0f, twoHanded);
            }
        }

        [HarmonyPatch(typeof(Weapon_Wand), "OnHeldTriggerPress", new Type[] { typeof(XRController) })]
        public class bhaptics_CastSpell
        {
            [HarmonyPostfix]
            public static void Postfix(Weapon_Wand __instance, XRController controller)
            {
                bool isRight = controller.IsRightController();
                tactsuitVr.CastSpell(isRight);
            }
        }

        [HarmonyPatch(typeof(Weapon_Bow), "OnGrabStopString", new Type[] { typeof(XRController) })]
        public class bhaptics_ShootBow
        {
            [HarmonyPostfix]
            public static void Postfix(Weapon_Bow __instance, XRController controller)
            {
                bool isRight = controller.IsRightController();
                tactsuitVr.ShootBow(isRight);
            }
        }

        [HarmonyPatch(typeof(Weapon_Blunt), "OnCollisionEnter", new Type[] { typeof(Collision) })]
        public class bhaptics_SwordCollide
        {
            [HarmonyPostfix]
            public static void Postfix(Weapon_Blunt __instance, Collision collision)
            {
                float speed = collision.relativeVelocity.magnitude;
                tactsuitVr.LOG("Sword speed: " + speed.ToString());
                tactsuitVr.SwordRecoil(true, speed/10.0f);
            }
        }

        private static (float, float) getAngleAndShift(Transform player, Vector3 hitPoint)
        {
            Vector3 patternOrigin = new Vector3(0f, 0f, 1f);
            // y is "up", z is "forward" in local coordinates
            Vector3 hitPosition = hitPoint - player.position;
            Quaternion PlayerRotation = player.rotation;
            Vector3 playerDir = PlayerRotation.eulerAngles;
            // We only want rotation correction in y direction (left-right), top-bottom and yaw we can leave
            Vector3 flattenedHit = new Vector3(hitPosition.x, 0f, hitPosition.z);
            float earlyhitAngle = Vector3.Angle(flattenedHit, patternOrigin);
            Vector3 earlycrossProduct = Vector3.Cross(flattenedHit, patternOrigin);
            if (earlycrossProduct.y > 0f) { earlyhitAngle *= -1f; }
            float myRotation = earlyhitAngle - playerDir.y;
            myRotation *= -1f;
            if (myRotation < 0f) { myRotation = 360f + myRotation; }

            float hitShift = hitPosition.y;
            if (hitShift > 0.0f) { hitShift = 0.5f; }
            else if (hitShift < -0.5f) { hitShift = -0.5f; }
            else { hitShift = (hitShift + 0.25f) * 2.0f; }

            return (myRotation, hitShift);
        }

        [HarmonyPatch(typeof(PlayerActor), "OnDamageApply", new Type[] { typeof(ProjectileHitInfo), typeof(ProjectileService.DamageResult) })]
        public class bhaptics_PlayerHit
        {
            [HarmonyPostfix]
            public static void Postfix(PlayerActor __instance, ProjectileHitInfo info, ProjectileService.DamageResult damageResult)
            {
                float hitAngle;
                float hitShift;
                string damageType = "BulletHit";
                if (info.damageData.instantKill) damageType = "Impact";
                if (info.damageData.isExplodingShot) damageType = "Impact";
                if ((info.damageData.isExplosion)||(info.damageData.isGrenade)) damageType = "Impact";
                if (info.damageData.isMelee) damageType = "BladeHit";
                if (info.damageData.isPoison) damageType = "Poison";
                if (info.damageData.isSpell) damageType = "Impact";
                (hitAngle, hitShift) = getAngleAndShift(__instance.transform, info.hitPosition);
                if (hitShift >= 0.5f) { tactsuitVr.HeadShot(hitAngle); return; }
                tactsuitVr.PlayBackHit(damageType, hitAngle, hitShift);
            }
        }

        [HarmonyPatch(typeof(HipAndFootIK), "OnFootTouchedGround", new Type[] { typeof(int), typeof(Vector3), typeof(Vector3) })]
        public class bhaptics_FootStep
        {
            [HarmonyPostfix]
            public static void Postfix(int index)
            {
                if (index == 0) tactsuitVr.PlaybackHaptics("FootStep_L");
                else tactsuitVr.PlaybackHaptics("FootStep_R");
            }
        }

        [HarmonyPatch(typeof(JuiceVolume), "FlashSettings", new Type[] { typeof(JuiceVolume.JuiceLayerName), typeof(float), typeof(float), typeof(float) })]
        public class bhaptics_Prayer
        {
            [HarmonyPostfix]
            public static void Postfix(JuiceVolume __instance, JuiceVolume.JuiceLayerName layerName)
            {
                if (layerName == JuiceVolume.JuiceLayerName.Prayer)
                {
                    tactsuitVr.PlaybackHaptics("PrayerHands");
                    tactsuitVr.PlaybackHaptics("PrayerArms");
                    tactsuitVr.PlaybackHaptics("PrayerVest");
                }
            }
        }



    }
}
