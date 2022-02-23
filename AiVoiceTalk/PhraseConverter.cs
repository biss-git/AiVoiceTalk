using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Yomiage.SDK.Talk;
using Yomiage.SDK.VoiceEffects;

namespace AiVoiceTalk
{
    internal static class PhraseConverter
    {
        /// <summary>
        /// A.I.VOICE では辞書登録のキーは全角のため、半角文字は全角に変換する。
        /// と思ったけど、半角でも問題なく動いてくれたので、何もしない。
        /// </summary>
        /// <param name="originalText"></param>
        /// <returns></returns>
        public static string GetKeyText(string originalText)
        {
            return originalText;
        }

        /// <summary>
        /// ユニコエの発声指示をA.I.VOICEの辞書に変換する。
        /// </summary>
        /// <param name="script">ユニコエの発声指示</param>
        /// <returns>A.I.VOICEの辞書</returns>
        public static string GetPhrase(TalkScript script)
        {
            var phrase = "$2_2";


            VoiceEffectValueBase currentEffect = new Section()
            {
                Volume = 1,
                Pitch = 1,
                Speed = 1,
                Emphasis = 1,
            };
            foreach (var section in script.Sections)
            {
                phrase += section.GetPhraseSection(section == script.Sections.FirstOrDefault(), currentEffect);
            }


            // 語尾のポーズ
            phrase += script.EndSection.Pause.GetPhrasePause();

            // 末尾の音声効果
            phrase += script.EndSection.GetPhraseEffect(currentEffect);

            // 語尾の種類
            switch (script.EndSection.EndSymbol)
            {
                case "。":
                    phrase += "<F>";
                    break;
                case "？":
                    phrase += "<R>";
                    break;
                case "！":
                    phrase += "<A>";
                    break;
                case "♪":
                    phrase += "<H>";
                    break;
                default:
                    phrase += "$2_2";
                    break;
            }

            return phrase;
        }

        private static string GetPhraseSection(this Section section, bool isFirst, VoiceEffectValueBase currentEffect)
        {
            if(section.Moras.Count == 0)
            {
                return "";
            }

            var phraseSection = "";

            // ポーズ
            if (section.Pause.Span_ms > 0)
            {
                phraseSection += section.Pause.GetPhrasePause();
            }
            else if (!isFirst)
            {
                // ポーズがない場合のアクセント句の区切り文字。
                phraseSection += "|0";
            }


            // 音声効果
            phraseSection += section.GetPhraseEffect(currentEffect);

            // 読みとアクセント句
            if (section.Moras.First().Accent || section.Moras.All(x => x.Accent == section.Moras.First().Accent))
            {
                phraseSection += "^";
            }
            phraseSection += section.Moras.First().GetPhraseMora();

            for (int i = 1; i < section.Moras.Count; i++)
            {
                var m1 = section.Moras[i - 1];
                var m2 = section.Moras[i];
                if(!m1.Accent && m2.Accent)
                {
                    phraseSection += "^";
                }
                if(m1.Accent && !m2.Accent)
                {
                    phraseSection += "!";
                }
                phraseSection += m2.GetPhraseMora();
            }


            return phraseSection;
        }

        private static string GetPhrasePause(this Pause pause)
        {
            if (pause.Span_ms <= 0)
            {
                return "";
            }
            else if (pause.Span_ms < 300 )
            {
                return $"$1_(Pau MSEC={pause.Span_ms})";
            }
            return $"$2_(Pau MSEC={pause.Span_ms})";
        }

        private static string GetPhraseMora(this Mora mora)
        {
            switch (mora.Voiceless)
            {
                case true:
                    return mora.Character + "D";
                case false:
                    return mora.Character + "V";
                default:
                    return mora.Character;
            }
        }

        private static string GetPhraseEffect(this VoiceEffectValueBase effect, VoiceEffectValueBase currentEffect)
        {
            var phrase = "";

            // 音量
            if (effect.Volume != null && effect.Volume != currentEffect.Volume)
            {
                phrase += $"(Vol ABSLEVEL={effect.Volume.Value.ToString("F2")})";
            }
            // スピード
            if (effect.Speed != null && effect.Speed != currentEffect.Speed)
            {
                phrase += $"(Spd ABSSPEED={effect.Speed.Value.ToString("F2")})";
            }
            // 高さ
            if (effect.Pitch != null && effect.Pitch != currentEffect.Pitch)
            {
                phrase += $"(Pit ABSLEVEL={effect.Pitch.Value.ToString("F2")})";
            }
            // 抑揚
            if (effect.Emphasis != null && effect.Emphasis != currentEffect.Emphasis)
            {
                phrase += $"(EMPH ABSLEVEL={effect.Emphasis.Value.ToString("F2")})";
            }

            {
                // 値を引き継ぐ
                currentEffect.Volume = effect.Volume.GetValueOrDefault(currentEffect.Volume.GetValueOrDefault(1));
                currentEffect.Speed = effect.Speed.GetValueOrDefault(currentEffect.Speed.GetValueOrDefault(1));
                currentEffect.Pitch = effect.Pitch.GetValueOrDefault(currentEffect.Pitch.GetValueOrDefault(1));
                currentEffect.Emphasis = effect.Emphasis.GetValueOrDefault(currentEffect.Emphasis.GetValueOrDefault(1));
            }

            return phrase;
        }
    }
}
