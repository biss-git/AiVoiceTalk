using AI.Talk.Editor.Api;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AiVoiceSave
{
    internal class Program
    {
        private static readonly string voicesFile = "voiceNames.txt";
        private static readonly string voiceFile = "voiceName.txt";
        private static readonly string textFile = "text.txt";
        // private static readonly string speedFile = "speed.txt";
        private static readonly string wavFile = "output.wav";

        static void Main(string[] args)
        {
            var _ttsControl = new TtsControl();// TTS APIの呼び出し用オブジェクト

            {
                // 接続先を探す
                var availableHosts = _ttsControl.GetAvailableHostNames();

                if (availableHosts.Count() > 0)
                {
                    // APIを初期化する
                    _ttsControl.Initialize(availableHosts[0]);
                }
                else
                {
                    return;
                }
            }

            {
                // 接続処理
                try
                {
                    if (_ttsControl.Status == HostStatus.NotRunning)
                    {
                        // ホストプログラムを起動する
                        _ttsControl.StartHost();
                    }

                    // ホストプログラムに接続する
                    _ttsControl.Connect();
                }
                catch (Exception ex)
                {
                    _ttsControl.Disconnect();
                    return;
                }
            }

            {
                // 見つけたプリセット名を列挙する
                var voiceNames = string.Empty;
                try
                {
                    foreach (var voice in _ttsControl.VoicePresetNames)
                    {
                        voiceNames += voice + Environment.NewLine;
                    }
                }
                catch (Exception)
                {
                    _ttsControl.Disconnect();
                    return;
                }
                File.WriteAllText(voicesFile, voiceNames);
            }

            {
                // 音声の指定を受け取る
                var voiceName = string.Empty;
                if (File.Exists(voiceFile))
                {
                    voiceName = File.ReadAllLines(voiceFile).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
                }

                if (!string.IsNullOrWhiteSpace(voiceName) &&
                    _ttsControl.VoicePresetNames.Contains(voiceName))
                {
                    // 音声を設定する
                    try
                    {
                        _ttsControl.CurrentVoicePresetName = voiceName;
                    }
                    catch (Exception)
                    {
                        _ttsControl.Disconnect();
                        return;
                    }
                }
            }

            {
                // マスターコントロールを設定するならここでやる

                //_ttsControl.MasterControl = 
            }

            {
                // 辞書を反映させるならここでやる

                // _ttsControl.ReloadPhraseDictionary();
            }

            {
                // 読み上げるテキストを受け取る
                var text = string.Empty;
                if (File.Exists(textFile))
                {
                    text = File.ReadAllText(textFile);
                }

                if (string.IsNullOrWhiteSpace(text))
                {
                    _ttsControl.Disconnect();
                    return;
                }

                // テキストを設定する
                try
                {
                    // テキスト編集モードにする　リストは面倒臭いため
                    _ttsControl.TextEditMode = TextEditMode.Text;

                    // テキスト
                    _ttsControl.Text = text;
                }
                catch (Exception ex)
                {
                    _ttsControl.Disconnect();
                    return;
                }
            }

            {
                // 音声を保存する。
                var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                path = Path.Combine(path, wavFile);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                _ttsControl.SaveAudioToFile(path);
            }

            _ttsControl.Disconnect();
        }

    }
}
