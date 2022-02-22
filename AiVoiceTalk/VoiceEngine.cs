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
        // private string speedFile => Path.Combine(DllDirectory, "speed.txt");
        private string wavFile => Path.Combine(DllDirectory, "output.wav");

        public override void Initialize(string configDirectory, string dllDirectory, EngineConfig config)
        {
            base.Initialize(configDirectory, dllDirectory, config);


            Excute();

        }
        public override async Task<double[]> Play(VoiceConfig mainVoice, VoiceConfig subVoice, TalkScript talkScript, MasterEffectValue masterEffect, Action<int> setSamplingRate_Hz, Action<double[]> submitWavePart)
        {
            await Task.Delay(10);

            if (File.Exists(voiceFile)) { File.Delete(voiceFile); }
            if (File.Exists(textFile)) { File.Delete(textFile); }
            // if (File.Exists(speedFile)) { File.Delete(speedFile); }
            if (File.Exists(wavFile)) { File.Delete(wavFile); }

            // 話者設定
            if (mainVoice.Library.Settings.Strings?.TryGetSetting("voiceName", out var setting) == true)
            {
                File.WriteAllText(voiceFile, setting.Value);
            }

            // マスターコントロールや辞書データ送るならここでやっておくのがいい

            // テキスト設定
            File.WriteAllText(textFile, talkScript.OriginalText);

            Excute();

            if (File.Exists(wavFile))
            {
                using var reader = new WaveFileReader(wavFile);
                int fs = reader.WaveFormat.SampleRate;
                setSamplingRate_Hz(fs);

                var wave = new List<double>(talkScript.Sections.First().Pause.Span_ms * fs / 1000);
                while (reader.Position < reader.Length)
                {
                    var samples = reader.ReadNextSampleFrame();
                    wave.Add(samples.First());
                }
                wave.AddRange(new double[(talkScript.EndSection.Pause.Span_ms + (int)masterEffect.EndPause) * fs / 1000]);
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

    }
}
