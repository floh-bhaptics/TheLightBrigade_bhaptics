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
using Unity.Mathematics;

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

        #region Weapons

        [HarmonyPatch(typeof(Weapon_Rifle), "TryFire", new Type[] {  })]
        public class bhaptics_RifleFire
        {
            [HarmonyPostfix]
            public static void Postfix(Weapon_Rifle __instance, bool ___boltOpenState, float ___nextShot, bool ___hammerOpenState)
            {
                if (___boltOpenState) { tactsuitVr.LOG("Bolt open"); return; }
                if ((UnityEngine.Object)__instance.nodeHammer != (UnityEngine.Object)null && ___hammerOpenState) { tactsuitVr.LOG("Hammer time!"); return; }
                if ((BaseConfig)__instance.chamber == (BaseConfig)null || __instance.chamberSpent) { tactsuitVr.LOG("Chamber empty"); return; }
                bool isRight = __instance.grabTrigger.gripController.IsRightController();
                bool twoHanded = false;
                //if ((UnityEngine.Object)__instance.grabBarrel != (UnityEngine.Object)null) twoHanded = true;
                //twoHanded = __instance.grabTrigger.alternateGrabAlso;
                twoHanded = ((UnityEngine.Object)__instance.grabBarrel != (UnityEngine.Object)null);
                tactsuitVr.GunRecoil(isRight, 1.0f, twoHanded);
            }
        }

        [HarmonyPatch(typeof(Weapon_Wand), "OnHeldTriggerRelease", new Type[] { typeof(XRController) })]
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

        #endregion

        #region Damage and Health

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
            tactsuitVr.LOG("Hitshift: " + hitShift.ToString());
            float upperBound = 1.7f;
            float lowerBound = 1.3f;
            if (hitShift > upperBound) { hitShift = 0.5f; }
            else if (hitShift < lowerBound) { hitShift = -0.5f; }
            // ...and then spread/shift it to [-0.5, 0.5], which is how bhaptics expects it
            else { hitShift = (hitShift - lowerBound) / (upperBound - lowerBound) - 0.5f; }

            return (myRotation, hitShift);
        }

        [HarmonyPatch(typeof(PlayerActor), "OnDamageApply", new Type[] { typeof(ProjectileHitInfo), typeof(ProjectileService.DamageResult) })]
        public class bhaptics_PlayerHit
        {
            [HarmonyPostfix]
            public static void Postfix(PlayerActor __instance, ProjectileHitInfo info, ProjectileService.DamageResult damageResult)
            {
                if (__instance.health <= 0.25f * __instance.maxHealth) tactsuitVr.StartHeartBeat();
                else tactsuitVr.StopHeartBeat();
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

        [HarmonyPatch(typeof(PlayerActor), "UpdateLowHealthState", new Type[] {  })]
        public class bhaptics_PlayerHealth
        {
            [HarmonyPostfix]
            public static void Postfix(PlayerActor __instance)
            {
                if (__instance.health <= 0.25f * __instance.maxHealth) tactsuitVr.StartHeartBeat();
                else tactsuitVr.StopHeartBeat();
            }
        }

        [HarmonyPatch(typeof(PlayerActor), "DoDeath", new Type[] { typeof(DeathEntry) })]
        public class bhaptics_PlayerDeath
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.StopThreads();
            }
        }

        #endregion

        #region Extra effects

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
            public static void Postfix(JuiceVolume __instance, JuiceVolume.JuiceLayerName layerName, float fadeInSecs, float holdDurationSecs, float fadeOutSecs)
            {
                if (layerName == JuiceVolume.JuiceLayerName.PlayerTeleport) tactsuitVr.PlaybackHaptics("Teleport");
                if (layerName == JuiceVolume.JuiceLayerName.AbsorbSoul) tactsuitVr.PlaybackHaptics("AbsorbSoul");
                if (layerName == JuiceVolume.JuiceLayerName.LevelUp) tactsuitVr.PlaybackHaptics("LevelUp");
                if (layerName == JuiceVolume.JuiceLayerName.AbsorbTarotCard) tactsuitVr.PlaybackHaptics("AbsorbTarotCard");
                if (layerName == JuiceVolume.JuiceLayerName.Prayer)
                {
                    if ((fadeInSecs == 0.1f) && (fadeOutSecs == 0.3f)) return;
                    //tactsuitVr.LOG("Numbers: " + fadeInSecs + " " + holdDurationSecs + " " + fadeOutSecs);
                    tactsuitVr.PlaybackHaptics("PrayerHands");
                    tactsuitVr.PlaybackHaptics("PrayerArms");
                    tactsuitVr.PlaybackHaptics("PrayerVest");
                }
            }
        }

        [HarmonyPatch(typeof(JuiceVolume), "TriggerAreaClearRingBurst", new Type[] {  })]
        public class bhaptics_AreaClear
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlaybackHaptics("AreaClear");
            }
        }

        [HarmonyPatch(typeof(JuiceVolume), "FadeOut", new Type[] { typeof(JuiceVolume.JuiceLayerName), typeof(float), typeof(float) })]
        public class bhaptics_FadeInLayer
        {
            [HarmonyPostfix]
            public static void Postfix(JuiceVolume __instance, JuiceVolume.JuiceLayerName layerName)
            {
                if (layerName == JuiceVolume.JuiceLayerName.PlayerTeleport) tactsuitVr.PlaybackHaptics("Teleport");
            }
        }

        #endregion
    }
}
