using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yomiage.SDK;
using Yomiage.SDK.Config;
using Yomiage.SDK.Talk;
using Yomiage.SDK.VoiceEffects;

namespace AiVoiceTalk
{
    public class VoiceEngine : VoiceEngineBase
    {
        private string exePath => Path.Combine(DllDirectory, "AiVoiceSave.exe");

        private string voicesFile => Path.Combine(DllDirectory, "voiceNames.txt");
        private string voiceFile => Path.Combine(DllDirectory, "voiceName.txt");
        private string textFile => Path.Combine(DllDirectory, "text.txt");
        private string wavFile => Path.Combine(DllDirectory, "output.wav");
        private string masterFile => Path.Combine(DllDirectory, "master.txt");
        private string phraseFile => Path.Combine(DllDirectory, "phrase.txt");
        private string phrasePathFile => Path.Combine(DllDirectory, "phrasePath.txt");

        public override void Initialize(string configDirectory, string dllDirectory, EngineConfig config)
        {
            base.Initialize(configDirectory, dllDirectory, config);

            if (File.Exists(voiceFile)) { File.Delete(voiceFile); }
            if (File.Exists(textFile)) { File.Delete(textFile); }
            if (File.Exists(wavFile)) { File.Delete(wavFile); }
            if (File.Exists(masterFile)) { File.Delete(masterFile); }
            if (File.Exists(phraseFile)) { File.Delete(phraseFile); }
            if (File.Exists(phrasePathFile)) { File.Delete(phrasePathFile); }

            var a = Settings;
            if (this.Settings.Strings?.TryGetSetting("PhrasePath", out var phrasePathSetting) == true &&
                (string.IsNullOrWhiteSpace(phrasePathSetting.Value) ||
                 !File.Exists(phrasePathSetting.Value)) &&
                (string.IsNullOrWhiteSpace(phrasePathSetting.DefaultValue) ||
                 !File.Exists(phrasePathSetting.DefaultValue)))
            {
                var pdic = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                pdic = Path.Combine(pdic, "A.I.VOICE Editor", "PhraseDictionaries", "user.pdic");
                if (File.Exists(pdic))
                {
                    phrasePathSetting.Value = pdic;
                    phrasePathSetting.DefaultValue = pdic;
                }
            }

            Excute();
        }
        public override async Task<double[]> Play(VoiceConfig mainVoice, VoiceConfig subVoice, TalkScript talkScript, MasterEffectValue masterEffect, Action<int> setSamplingRate_Hz, Action<double[]> submitWavePart)
        {
            await Task.Delay(10);

            if (File.Exists(voiceFile)) { File.Delete(voiceFile); }
            if (File.Exists(textFile)) { File.Delete(textFile); }
            if (File.Exists(wavFile)) { File.Delete(wavFile); }
            if (File.Exists(masterFile)) { File.Delete(masterFile); }
            if (File.Exists(phraseFile)) { File.Delete(phraseFile); }
            if (File.Exists(phrasePathFile)) { File.Delete(phrasePathFile); }

            // 話者設定
            if (mainVoice.Library.Settings.Strings?.TryGetSetting("voiceSelectType", out var voiceSelectSetting) == true)
            {
                if (voiceSelectSetting.Value == "ボイスプリセット指定" &&
                    mainVoice.Library.Settings.Strings?.TryGetSetting("voiceName", out var voiceNameSetting) == true)
                {
                    // ボイスプリセット名で指定
                    File.WriteAllText(voiceFile, voiceNameSetting.Value);
                }
                if(voiceSelectSetting.Value == "ユニコエのボイスプリセットを使用")
                {
                    // ユニコエでボイスプリセットを使用する場合はここで設定
                }
                else
                {
                    // A.I.VOICEのプリセットを使用する場合は、ユニコエのプリセットの音声効果はA.I.VOICEのマスターコントロールに含める。
                    masterEffect.Volume = masterEffect.Volume.GetValueOrDefault(1) * mainVoice.VoiceEffect.Volume.GetValueOrDefault(1);
                    masterEffect.Speed = masterEffect.Speed.GetValueOrDefault(1) * mainVoice.VoiceEffect.Speed.GetValueOrDefault(1);
                    masterEffect.Pitch = masterEffect.Pitch.GetValueOrDefault(1) * mainVoice.VoiceEffect.Pitch.GetValueOrDefault(1);
                    masterEffect.Emphasis = masterEffect.Emphasis.GetValueOrDefault(1) * mainVoice.VoiceEffect.Emphasis.GetValueOrDefault(1);
                }
            }

            // マスターコントロール の設定
            File.WriteAllText(masterFile, MasterContorolToJson(masterEffect));

            // 辞書データ の設定
            if (this.Settings.Strings?.TryGetSetting("PhrasePath", out var phrasePathSetting) == true &&
                !string.IsNullOrWhiteSpace(phrasePathSetting.Value) &&
                File.Exists(phrasePathSetting.Value))
            {
                File.WriteAllText(phrasePathFile, phrasePathSetting.Value);

                File.WriteAllText(phraseFile,
                    "# ComponentName=\"AITalk\" ComponentVersion=\"6.0.0.0\" UpdateDateTime=\"2222 / 02 / 22 22:22:22.222\" Type=\"Phrase\" Version=\"3.3\" Language=\"Japanese\" Count=\"1\"" + Environment.NewLine +
                    "num:0" + Environment.NewLine +
                    PhraseConverter.GetKeyText(talkScript.OriginalText) + Environment.NewLine +
                    PhraseConverter.GetPhrase(talkScript)
                    );
            }

            // テキスト設定
            File.WriteAllText(textFile, talkScript.OriginalText);

            Excute();

            if (File.Exists(wavFile))
            {
                using var reader = new WaveFileReader(wavFile);
                int fs = reader.WaveFormat.SampleRate;
                setSamplingRate_Hz(fs);

                var wave = new List<double>();
                //var wave = new List<double>(talkScript.Sections.First().Pause.Span_ms * fs / 1000);
                while (reader.Position < reader.Length)
                {
                    var samples = reader.ReadNextSampleFrame();
                    wave.Add(samples.First());
                }
                wave.AddRange(new double[((int)masterEffect.EndPause) * fs / 1000]);
                return wave.ToArray();
            }

            return new double[0];
        }


        private void Excute()
        {
            if (!File.Exists(exePath))
            {
                return;
            }

            var processStartInfo = new ProcessStartInfo()
            {
                FileName = exePath,
                CreateNoWindow = true,
                UseShellExecute = false,
                WorkingDirectory = DllDirectory,
            };
            var process = Process.Start(processStartInfo);
            process.WaitForExit();
            process.Dispose();
        }


        private static string MasterContorolToJson(MasterEffectValue masterEffect)
        {
            return "{ \"Volume\" : " + Math.Clamp(masterEffect.Volume.GetValueOrDefault(1.00), 0, 5) + ", " +
                "\"Pitch\" : " + Math.Clamp(masterEffect.Pitch.GetValueOrDefault(1.00), 0, 5) + ", " +
                "\"Speed\" : " + Math.Clamp(masterEffect.Speed.GetValueOrDefault(1.00), 0, 5) + ", " +
                "\"PitchRange\" : " + Math.Clamp(masterEffect.Emphasis.GetValueOrDefault(1.00), 0, 5) + ", " +
                "\"MiddlePause\" : " + Math.Clamp(masterEffect.ShortPause, 80, 500).ToString("F0") + ", " +
                "\"LongPause\" : " + Math.Clamp(masterEffect.LongPause, 80, 2000).ToString("F0") + ", " +
                "\"SentencePause\" : " + Math.Clamp(masterEffect.EndPause, 0, 10000).ToString("F0") + " }";
        }
    }
}
