using BepInEx;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Profiling;
using Vosk;

namespace VoskVoiceRecognitionAPI
{
    internal class SpeechHandler
    {

        internal SpeechHandler()
        {
            Model a = new Model("vosk-model-small-en-us-0.15");
        }
    }
}
