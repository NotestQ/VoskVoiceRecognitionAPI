using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using VoskVoiceRecognitionAPI;

namespace VoiceRecognitionAPI.Patches {
    [HarmonyPatch(typeof(NetworkVoiceHandler))]
    internal class NetworkVoiceHandlerPatch {
        [HarmonyPostfix, HarmonyPatch("Start")]
        internal static void SetupRecognitionEngine() {
            if (VoskPlugin.CanStart) 
                new SpeechHandler();
        }
    }
}
