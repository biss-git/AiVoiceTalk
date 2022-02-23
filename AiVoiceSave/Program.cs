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
        private static readonly string wavFile = "output.wav";
        private static readonly string masterFile = "master.txt";
        private static readonly string phraseFile = "phrase.txt";
        private static readonly string phrasePathFile = "phrasePath.txt";

        static void Main(string[] args)
        {
            var _ttsControl = new TtsControl();// TTS APIの呼び出し用オブジェクト
            var originalName = "";
            var originalMaster = "";
            var originalPhraseDic = "";
            var phrasePath = "";

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
                    Disconnect(_ttsControl, originalMaster, originalName, phrasePath, originalPhraseDic);
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
                    Disconnect(_ttsControl, originalMaster, originalName, phrasePath, originalPhraseDic);
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
                        originalName = _ttsControl.CurrentVoicePresetName;
                        _ttsControl.CurrentVoicePresetName = voiceName;
                    }
                    catch (Exception)
                    {
                        Disconnect(_ttsControl, originalMaster, originalName, phrasePath, originalPhraseDic);
                        return;
                    }
                }
            }

            {
                // マスターコントロールを設定するならここでやる

                var master = string.Empty;
                if (File.Exists(masterFile))
                {
                    master = File.ReadAllText(masterFile);
                }

                if (!string.IsNullOrWhiteSpace(master))
                {
                    originalMaster = _ttsControl.MasterControl;
                    try
                    {
                        _ttsControl.MasterControl = master;
                    }
                    catch
                    {
                        Disconnect(_ttsControl, originalMaster, originalName, phrasePath, originalPhraseDic);
                        return;
                    }
                }
            }

            {
                // 辞書を反映させる
                if (File.Exists(phrasePathFile) && File.Exists(phraseFile))
                {
                    phrasePath = File.ReadAllText(phrasePathFile);
                    var phraseDic = File.ReadAllText(phraseFile);
                    if (File.Exists(phrasePath))
                    {
                        try
                        {
                            originalPhraseDic = File.ReadAllText(phrasePath, Encoding.GetEncoding("shift_jis"));
                            File.WriteAllText(phrasePath, phraseDic, Encoding.GetEncoding("shift_jis"));
                            _ttsControl.ReloadPhraseDictionary();
                        }
                        catch (Exception)
                        {
                            Disconnect(_ttsControl, originalMaster, originalName, phrasePath, originalPhraseDic);
                            return;
                        }
                    }
                }
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
                    Disconnect(_ttsControl, originalMaster, originalName, phrasePath, originalPhraseDic);
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
                    Disconnect(_ttsControl, originalMaster, originalName, phrasePath, originalPhraseDic);
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

            Disconnect(_ttsControl, originalMaster, originalName, phrasePath, originalPhraseDic);
        }


        private static void Disconnect(TtsControl _ttsControl, string masterJson, string voiceName, string phrasePath, string phraseDic)
        {
            // キャラクター選択を元に戻す
            if (!string.IsNullOrEmpty(voiceName))
            {
                try
                {
                    _ttsControl.CurrentVoicePresetName = voiceName;
                }
                catch (Exception)
                {
                }
            }

            // マスターコントロールをもとに戻す
            if (!string.IsNullOrWhiteSpace(masterJson))
            {
                try
                {
                    _ttsControl.MasterControl = masterJson;
                }
                catch (Exception)
                {
                }
            }

            // フレーズ辞書をもとに戻す
            if (!string.IsNullOrWhiteSpace(phrasePath) &&
                File.Exists(phrasePath) &&
                !string.IsNullOrWhiteSpace(phraseDic))
            {
                try
                {
                    File.WriteAllText(phrasePath, phraseDic, Encoding.GetEncoding("shift_jis"));
                    _ttsControl.ReloadPhraseDictionary();
                }
                catch (Exception)
                {
                }
            }

            _ttsControl.Disconnect();
        }

    }
}
