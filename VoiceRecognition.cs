/* * * * *
 * Voice recognition grammar set up
 * ------------------------------
 *
 * A script to handle 
 *
 * Originally written by LoafOrc
 * License comment added by Notest
 *
 * GNU GPL 3.0 License
 *
 *
 * Licensed under the GNU General Public License, Version 3.0 (the "License");
 *   you may not use this file except in compliance with the License.
 *   You may obtain a copy of the License at
 *
 *   https://www.gnu.org/licenses/gpl-3.0.html
 *
 *   A copy of the license is located in the "VOICERECOGNITION-LICENSE" file accompanying this source.
 *
 *   Unless required by applicable law or agreed to in writing, software
 *   distributed under the License is distributed on an "AS IS" BASIS,
 *   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *   See the License for the specific language governing permissions and
 *   limitations under the License.
 *
 * * * * */

/* * * * *
 * Modifications
 * ----------------------
 *
 * 2024 - Notest
 * Changed confidence const to "DefaultMinimumTotalConfidence"
 * Added checks for words that are not recognizable by a model
 * Added methods to listen for speech instead of only phrases
 * Added event for speech
 * Added comments
 * Spearated main recognition method into speech recognition and phrase recognition
 *
 * * * * */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace VoskVoiceRecognitionAPI
{
    public static class VoiceRecognition
    {
        public const float DefaultMinimumTotalConfidence = .2f;
        public const float DefaultMinimumWordConfidence = 0f; // TODO: will remove words from text that are below confidence
        public static int EventsListeningForPhrases = 0;
        public static int EventsListeningForSpeech = 0;

        internal static event EventHandler<VoiceRecognitionEventArgs> PhraseRecognitionFinishedEvent = (__, args) => {
            VoskPlugin.Logger.LogDebug("Phrase recognized: \"" + args.Message + "\" with a confidence of " + args.TotalConfidence);
        };

        internal static event EventHandler<VoiceRecognitionEventArgs> SpeechRecognitionFinishedEvent = (__, args) => {
            VoskPlugin.Logger.LogDebug("Speech recognized: \"" + args.Message + "\" with a confidence of " + args.TotalConfidence);
        };

        internal static List<string> phraseList = new List<string>();

        public static EventHandler<VoiceRecognitionEventArgs> CustomListenForPhrases(string[] phrases, EventHandler<VoiceRecognitionEventArgs> callback)
        {
            string[] newPhraseList = new string[] {};

            foreach (string phrase in phrases)
            {
                bool canAddPhrase = true;
                foreach (string word in phrase.Split(" "))
                {
                    if (VoskPlugin.voskModel.FindWord(word) == -1)
                    {
                        canAddPhrase = false;
                        VoskPlugin.Logger.LogWarning($"Could not add phrase {phrase} because word {word} is not recognizable by current model");
                        break;
                    }
                }

                if (canAddPhrase) 
                    newPhraseList.Append(phrase);
            }

            if (newPhraseList.Length < 1)
            {
                VoskPlugin.Logger.LogWarning("No phrases to listen to...");
            }
            else
            {
                EventsListeningForPhrases++;
            }

            phraseList.AddRange(newPhraseList);
            EventHandler<VoiceRecognitionEventArgs> wrapped = (__, args) => {
                if (newPhraseList.Contains(args.Message))
                {
                    callback.Invoke(__, args);
                }
            };
            PhraseRecognitionFinishedEvent += wrapped;
            return wrapped;
        }

        public static EventHandler<VoiceRecognitionEventArgs> ListenForPhrase(string phrase, Action<string> callback)
        {
            return ListenForPhrase(phrase, DefaultMinimumTotalConfidence, callback);
        }

        public static EventHandler<VoiceRecognitionEventArgs> ListenForPhrase(string phrase, float minConfidence, Action<string> callback)
        {
            return ListenForPhrases(new string[] { phrase }, minConfidence, callback);
        }

        public static EventHandler<VoiceRecognitionEventArgs> ListenForPhrases(string[] phrases, Action<string> callback)
        {
            return ListenForPhrases(phrases, DefaultMinimumTotalConfidence, callback);
        }

        public static EventHandler<VoiceRecognitionEventArgs> ListenForPhrases(string[] phrases, float minConfidence, Action<string> callback)
        {
            string[] newPhraseList = new string[] { };

            foreach (string phrase in phrases)
            {
                bool canAddPhrase = true;
                foreach (string word in phrase.Split(" "))
                {
                    if (VoskPlugin.voskModel.FindWord(word) == -1)
                    {
                        canAddPhrase = false;
                        VoskPlugin.Logger.LogWarning($"Could not add phrase {phrase} because word {word} is not recognizable by current model");
                        break;
                    }
                }

                if (canAddPhrase)
                    newPhraseList.Append(phrase);
            }

            if (newPhraseList.Length < 1)
            {
                VoskPlugin.Logger.LogWarning("No phrases to listen to...");
            }
            else
            {
                EventsListeningForPhrases++;
            }
            
            phraseList.AddRange(newPhraseList);
            EventHandler<VoiceRecognitionEventArgs> recCallback = (__, args) => {
                if (newPhraseList.Contains(args.Message) && args.TotalConfidence >= minConfidence)
                {
                    callback.Invoke(args.Message!);
                }
            };
            PhraseRecognitionFinishedEvent += recCallback;
            return recCallback;
        }
        public static EventHandler<VoiceRecognitionEventArgs> Listen(Action<string> callback)
        {
            return Listen(DefaultMinimumTotalConfidence, callback);
        }
        public static EventHandler<VoiceRecognitionEventArgs> Listen(float minConfidence, Action<string> callback)
        {
            EventsListeningForSpeech++;
            EventHandler<VoiceRecognitionEventArgs> recCallback = (__, args) => {
                if (args.TotalConfidence >= minConfidence)
                {
                    callback.Invoke(args.Message!);
                }
            };
            SpeechRecognitionFinishedEvent += recCallback;
            return recCallback;
        }

        public static void StopListeningForSpeech(EventHandler<VoiceRecognitionEventArgs> callback)
        {
            EventsListeningForSpeech--;
            PhraseRecognitionFinishedEvent -= callback;
        }

        public static void StopListeningForPhrase(EventHandler<VoiceRecognitionEventArgs> callback)
        {
            EventsListeningForPhrases--;
            PhraseRecognitionFinishedEvent -= callback;
        }

        internal static void SpeechRecognition(RecognitionResult recognitionResult)
        {
            VoiceRecognitionEventArgs args = new VoiceRecognitionEventArgs();

            /* TODO: Make this an option maybe? RegEx that removes [unk]
             * args.Message = Regex.Replace(recognitionResult.text, "\\s?\\[unk\\](?!\\s)|(?<!\\s)\\[unk\\]\\s?", "");
             * args.Message = Regex.Replace(args.Message, "\\s[unk]\\s", " ");
             */

            int iteration = 0;
            float confidenceSum = 0;
            foreach (WordResult wordResult in recognitionResult.result)
            {
                iteration++;
                confidenceSum += wordResult.conf;
            }

            args.TotalConfidence = confidenceSum / iteration; // Normalize the confidence on each word
            try
            {
                SpeechRecognitionFinishedEvent.Invoke(VoskPlugin.Instance, args);
            }
            catch (Exception ex)
            {
                VoskPlugin.Logger.LogError("Something failed to do something " + ex.Message + "\n" + ex.StackTrace);
            }
        }

        internal static void PhraseRecognition(RecognitionResult recognitionResult)
        {
            VoiceRecognitionEventArgs args = new VoiceRecognitionEventArgs();

            args.Message = recognitionResult.text;
            int iteration = 0;
            float confidenceSum = 0;
            foreach (WordResult wordResult in recognitionResult.result)
            {
                iteration++;
                confidenceSum += wordResult.conf;
            }

            args.TotalConfidence = confidenceSum / iteration; // Normalize the confidence on each word
            try
            {
                PhraseRecognitionFinishedEvent.Invoke(VoskPlugin.Instance, args);
            }
            catch (Exception ex)
            {
                VoskPlugin.Logger.LogError("Something failed to do something " + ex.Message + "\n" + ex.StackTrace);
            }
        }

        public class VoiceRecognitionEventArgs : EventArgs
        {
            public string Message;
            public float TotalConfidence;
        }
    }
}
