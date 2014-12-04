﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Nikse.SubtitleEdit.Logic.SubtitleFormats
{
    public class SubViewer20 : SubtitleFormat
    {
        private enum ExpectingLine
        {
            TimeCodes,
            Text
        }

        public override string Extension
        {
            get { return ".sub"; }
        }

        public override string Name
        {
            get { return "SubViewer 2.0"; }
        }

        public override bool IsTimeBased
        {
            get { return true; }
        }

        public override bool IsMine(List<string> lines, string fileName)
        {
            var subtitle = new Subtitle();
            LoadSubtitle(subtitle, lines, fileName);
            return subtitle.Paragraphs.Count > _errorCount;
        }

        public override string ToText(Subtitle subtitle, string title)
        {
            const string paragraphWriteFormat = "{0:00}:{1:00}:{2:00}.{3:00},{4:00}:{5:00}:{6:00}.{7:00}{8}{9}";
            const string header = @"[INFORMATION]
[TITLE]{0}
[AUTHOR]
[SOURCE]
[PRG]
[FILEPATH]
[DELAY]0
[CD TRACK]0
[COMMENT]
[END INFORMATION]
[SUBTITLE]
[COLF]&H000000,[STYLE]bd,[SIZE]25,[FONT]Arial
";
            //00:00:06.61,00:00:13.75
            //text1[br]text2
            var sb = new StringBuilder();
            sb.AppendFormat(header, title);
            foreach (Paragraph p in subtitle.Paragraphs)
            {
                string text = p.Text.Replace(Environment.NewLine, "[br]");
                text = text.Replace("<i>", "{\\i1}");
                text = text.Replace("</i>", "{\\i0}");
                text = text.Replace("</i>", "{\\i}");
                text = text.Replace("<b>", "{\\b1}'");
                text = text.Replace("</b>", "{\\b0}");
                text = text.Replace("</b>", "{\\b}");
                text = text.Replace("<u>", "{\\u1}");
                text = text.Replace("</u>", "{\\u0}");
                text = text.Replace("</u>", "{\\u}");

                sb.AppendLine(string.Format(paragraphWriteFormat,
                                        p.StartTime.Hours,
                                        p.StartTime.Minutes,
                                        p.StartTime.Seconds,
                                        RoundTo2Cifres(p.StartTime.Milliseconds),
                                        p.EndTime.Hours,
                                        p.EndTime.Minutes,
                                        p.EndTime.Seconds,
                                        RoundTo2Cifres(p.EndTime.Milliseconds),
                                        Environment.NewLine,
                                        text));
                sb.AppendLine();
            }
            return sb.ToString().Trim();
        }

        private static int RoundTo2Cifres(int milliseconds)
        {
            int rounded = (int)Math.Round((double)milliseconds / 10);
            return rounded;
        }

        public override void LoadSubtitle(Subtitle subtitle, List<string> lines, string fileName)
        {
            var regexTimeCodes = new Regex(@"^\d\d:\d\d:\d\d.\d+,\d\d:\d\d:\d\d.\d+$", RegexOptions.Compiled);

            var paragraph = new Paragraph();
            ExpectingLine expecting = ExpectingLine.TimeCodes;
            _errorCount = 0;

            subtitle.Paragraphs.Clear();
            foreach (string line in lines)
            {
                if (regexTimeCodes.IsMatch(line))
                {
                    var parts = line.Split(new[] { ':', ',', '.' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 8)
                    {
                        paragraph.StartTime = TimeCode.FromTimestampTokens(parts[0], parts[1], parts[2], parts[3]);
                        paragraph.EndTime = TimeCode.FromTimestampTokens(parts[4], parts[5], parts[6], parts[7]);
                        expecting = ExpectingLine.Text;
                    }
                }
                else
                {
                    if (expecting == ExpectingLine.Text)
                    {
                        if (line.Length > 0)
                        {
                            string text = line.Replace("[br]", Environment.NewLine);
                            text = text.Replace("{\\i1}", "<i>");
                            text = text.Replace("{\\i0}", "</i>");
                            text = text.Replace("{\\i}", "</i>");
                            text = text.Replace("{\\b1}", "<b>'");
                            text = text.Replace("{\\b0}", "</b>");
                            text = text.Replace("{\\b}", "</b>");
                            text = text.Replace("{\\u1}", "<u>");
                            text = text.Replace("{\\u0}", "</u>");
                            text = text.Replace("{\\u}", "</u>");

                            paragraph.Text = text;
                            subtitle.Paragraphs.Add(paragraph);
                            paragraph = new Paragraph();
                            expecting = ExpectingLine.TimeCodes;
                        }
                    }
                }
            }
            subtitle.Renumber(1);
        }
    }
}
