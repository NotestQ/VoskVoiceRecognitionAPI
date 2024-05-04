using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Vosk;
using Pv.Unity;
using Newtonsoft.Json;

namespace VoskVoiceRecognitionAPI
{
    internal class SpeechHandler
    {
        internal VoiceProcessor voiceProcessor = VoiceProcessor.Instance;

        private static ConcurrentQueue<short[]> _bufferQueue = new ConcurrentQueue<short[]>();
        private static List<short> _buffer = new List<short>();
        private static VoskRecognizer? voskGrammarRecognizer;
        private static VoskRecognizer? voskRecognizer;

        public static float MaximumRecordingLength = 1;

        private static float _recordingLength;
        private static bool _running;

        internal SpeechHandler()
        {
            int sampleRate = 16000;
            Microphone.GetDeviceCaps(voiceProcessor.CurrentDeviceIndex, out int minimumSample, out int maximumSample);
            if (sampleRate > maximumSample ) // E.g. maximumSample is 8k and min is 6k, we set sampleRate to 8k
            {
                sampleRate = maximumSample;
            }

            if (minimumSample > sampleRate)  // E.g. minimumSample is 48k, we set sampleRate to 48k
            {
                sampleRate = minimumSample;
            }

            VoskPlugin.Logger.LogDebug($"Minimum sample rate: {minimumSample} | Maximum sample rate: {maximumSample}");
            VoskPlugin.Logger.LogWarning(VoskPlugin.voskModel.FindWord("nordvpn"));
            if (VoiceRecognition.phraseList.Count > 0)
            {
                VoiceRecognition.phraseList.Add("[unk]");
                var grammar = JsonConvert.SerializeObject(VoiceRecognition.phraseList);
                VoskPlugin.Logger.LogWarning(grammar);
                voskGrammarRecognizer = new VoskRecognizer(VoskPlugin.voskModel, (float)sampleRate, grammar);
                voskGrammarRecognizer.SetWords(true);
                VoskPlugin.Logger.LogDebug("Grammar model loaded!");
            }

            if (VoiceRecognition.EventsListeningForSpeech > 0)
            {
                voskRecognizer = new VoskRecognizer(VoskPlugin.voskModel, (float)sampleRate);
                VoskPlugin.Logger.LogDebug("Speech model loaded!");
            }

            voiceProcessor.AddFrameListener(FrameListener);
            voiceProcessor.StartRecording(512, sampleRate);
        }

        private static void FrameListener(short[] samples)
        {
            if (_buffer.Count == 0)
            {
                _recordingLength = Time.time;
            }

            if ((Time.time - _recordingLength) > MaximumRecordingLength)
            {
                _bufferQueue.Enqueue(_buffer.ToArray()); // Will enqueue even if we're still running
                Task.Run(ThreadedWork).ConfigureAwait(false);
                _buffer.Clear();

                return;
            }

            _buffer.AddRange(samples);
        }

        private static async Task ThreadedWork()
        {
            if (_running) return;
            _running = true;

            while (_bufferQueue.Count > 0)
            {
                if (_bufferQueue.TryDequeue(out short[] voiceResult))
                {
                    if (VoiceRecognition.EventsListeningForSpeech > 0)
                    {
                        if (voskRecognizer.AcceptWaveform(voiceResult, voiceResult.Length))
                        {
                            string resultStr = voskRecognizer.Result();
                            RecognitionResult result = JsonConvert.DeserializeObject<RecognitionResult>(resultStr)!;
                            VoiceRecognition.SpeechRecognition(result);
                            VoskPlugin.Logger.LogWarning($"{resultStr}");
                        }
                    }

                    if (VoiceRecognition.EventsListeningForPhrases > 0)
                    {
                        if (voskGrammarRecognizer.AcceptWaveform(voiceResult, voiceResult.Length))
                        {
                            string resultStr = voskGrammarRecognizer.Result();
                            RecognitionResult result = JsonConvert.DeserializeObject<RecognitionResult>(resultStr)!;
                            VoiceRecognition.PhraseRecognition(result);
                            VoskPlugin.Logger.LogWarning($"{resultStr}");
                        }
                    }
                }
            }

            _running = false;
        }
    }
}
