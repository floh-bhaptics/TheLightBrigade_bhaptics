using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MelonLoader;
using HarmonyLib;
using MyBhapticsTactsuit;
using LB;

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
    }
}
