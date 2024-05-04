using System;
using System.Collections.Generic;
using System.Text;

namespace VoskVoiceRecognitionAPI
{
    internal class WordResult
    {
        public float conf;
        public float end;
        public float start;
        public string word;
    }
    internal class RecognitionResult
    {
        public List<WordResult>? result;
        public string text;
    }
}
