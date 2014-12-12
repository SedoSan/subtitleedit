﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Nikse.SubtitleEdit.Logic.SubtitleFormats
{
    public class UnknownSubtitle22 : SubtitleFormat
    {

        //25    10:03:20:23 02:07   10:03:23:05
        //I see, on my way.
        //
        //26    10:03:31:18 02:07   10:03:34:00
        //Panessa, why didn't they give them
        //an escape route ?

        private static readonly Regex RegexTimeCode = new Regex(@"^\d+\t\d\d:\d\d:\d\d:\d\d\t\d\d:\d\d\t\d\d:\d\d:\d\d:\d\d$", RegexOptions.Compiled);

        public override string Extension
        {
            get { return ".txt"; }
        }

        public override string Name
        {
            get { return "Unknown 22"; }
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

        private static string MakeTimeCode(TimeCode tc)
        {
            return string.Format("{0:00}:{1:00}:{2:00}:{3:00}", tc.Hours, tc.Minutes, tc.Seconds, MillisecondsToFramesMaxFrameRate(tc.Milliseconds));
        }

        public override string ToText(Subtitle subtitle, string title)
        {
            var sb = new StringBuilder();
            sb.AppendLine("#\tAppearance\tCaption\t");
            sb.AppendLine();
            int count = 1;
            for (int i = 0; i < subtitle.Paragraphs.Count; i++)
            {
                Paragraph p = subtitle.Paragraphs[i];
                string text = Utilities.RemoveHtmlTags(p.Text);
                sb.AppendLine(string.Format("{0}\t{1}\t{2:00}:{3:00}\t{4}\r\n{5}\r\n", count, MakeTimeCode(p.StartTime), p.Duration.Seconds, MillisecondsToFramesMaxFrameRate(p.Duration.Milliseconds), MakeTimeCode(p.EndTime), text));
                count++;
            }

            return sb.ToString();
        }

        private static TimeCode DecodeTimeCode(string timestamp)
        {
            var tokens = timestamp.Split(new[] { ':', ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
            return TimeCode.FromFrameTokens(tokens[0], tokens[1], tokens[2], tokens[3]);
        }

        public override void LoadSubtitle(Subtitle subtitle, List<string> lines, string fileName)
        {
            _errorCount = 0;
            Paragraph p = null;
            var sb = new StringBuilder();
            foreach (string line in lines)
            {
                string s = line.TrimEnd();
                if (RegexTimeCode.IsMatch(s))
                {
                    try
                    {
                        if (p != null)
                        {
                            p.Text = sb.ToString().Trim();
                            subtitle.Paragraphs.Add(p);
                        }
                        sb = new StringBuilder();
                        string[] arr = s.Split('\t');
                        if (arr.Length == 4)
                            p = new Paragraph(DecodeTimeCode(arr[1]), DecodeTimeCode(arr[3]), string.Empty);
                    }
                    catch
                    {
                        _errorCount++;
                        p = null;
                    }
                }
                else if (!string.IsNullOrWhiteSpace(s))
                {
                    sb.AppendLine(s);
                }
            }
            if (p != null)
            {
                p.Text = sb.ToString().Trim();
                subtitle.Paragraphs.Add(p);
            }
            subtitle.Renumber(1);
        }

    }
}
