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
    }
}
