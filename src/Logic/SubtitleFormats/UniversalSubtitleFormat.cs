﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Nikse.SubtitleEdit.Logic.SubtitleFormats
{
    public class UniversalSubtitleFormat : SubtitleFormat
    {
        public override string Extension
        {
            get { return ".usf"; }
        }

        public override string Name
        {
            get { return "Universal Subtitle Format"; }
        }

        public override bool IsTimeBased
        {
            get { return true; }
        }

        public override bool IsMine(List<string> lines, string fileName)
        {
            var subtitle = new Subtitle();
            LoadSubtitle(subtitle, lines, fileName);
            return subtitle.Paragraphs.Count > 0;
        }

        public override string ToText(Subtitle subtitle, string title)
        {
            string xmlStructure =
                "<?xml version=\"1.0\" encoding=\"utf-8\" ?>" + Environment.NewLine +
                "<USFSubtitles version=\"1.0\">" + Environment.NewLine +
                @"<metadata>
    <title>Universal Subtitle Format</title>
    <author>
      <name>SubtitleEdit</name>
      <email>nikse.dk@gmail.com</email>
      <url>http://www.nikse.dk/</url>
    </author>" + Environment.NewLine +
"   <language code=\"eng\">English</language>" + Environment.NewLine +
@"  <date>[DATE]</date>
    <comment>This is a USF file</comment>
  </metadata>
  <styles>
    <!-- Here we redefine the default style -->" + Environment.NewLine +
                "    <style name=\"Default\">" + Environment.NewLine +
                "      <fontstyle face=\"Arial\" size=\"24\" color=\"#FFFFFF\" back-color=\"#AAAAAA\" />" +
                Environment.NewLine +
                "      <position alignment=\"BottomCenter\" vertical-margin=\"20%\" relative-to=\"Window\" />" +
                @"    </style>
  </styles>

  <subtitles>
  </subtitles>
</USFSubtitles>";
            xmlStructure = xmlStructure.Replace("[DATE]", DateTime.Now.ToString("yyyy-MM-dd"));

            var xml = new XmlDocument();
            xml.LoadXml(xmlStructure);
            xml.DocumentElement.SelectSingleNode("metadata/title").InnerText = title;
            var subtitlesNode = xml.DocumentElement.SelectSingleNode("subtitles");

            foreach (Paragraph p in subtitle.Paragraphs)
            {
                XmlNode paragraph = xml.CreateElement("subtitle");

                XmlAttribute start = xml.CreateAttribute("start");
                start.InnerText = p.StartTime.ToString().Replace(",", ".");
                paragraph.Attributes.Prepend(start);

                XmlAttribute stop = xml.CreateAttribute("stop");
                stop.InnerText = p.EndTime.ToString().Replace(",", ".");
                paragraph.Attributes.Append(stop);

                XmlNode text = xml.CreateElement("text");
                text.InnerText = Utilities.RemoveHtmlTags(p.Text);
                paragraph.AppendChild(text);

                XmlAttribute style = xml.CreateAttribute("style");
                style.InnerText = "Default";
                text.Attributes.Append(style);

                subtitlesNode.AppendChild(paragraph);
            }

            return ToUtf8XmlString(xml);
        }

        private static TimeCode DecodeTimeCode(string timestamp)
        {
            // hh:mm:ss:ff
            var tokens = timestamp.Split(new[] { ':', '.', ',' }, StringSplitOptions.RemoveEmptyEntries);
            return TimeCode.FromFrameTokens(tokens[0], tokens[1], tokens[2], tokens[3]);
        }

        public override void LoadSubtitle(Subtitle subtitle, List<string> lines, string fileName)
        {
            _errorCount = 0;

            var sb = new StringBuilder();
            lines.ForEach(line => sb.AppendLine(line));

            string xmlString = sb.ToString();
            if (!xmlString.Contains("<USFSubtitles") || !xmlString.Contains("<subtitles>"))
                return;

            var xml = new XmlDocument();
            xml.XmlResolver = null;
            try
            {
                xml.LoadXml(xmlString);
            }
            catch
            {
                _errorCount = 1;
                return;
            }

            foreach (XmlNode node in xml.DocumentElement.SelectNodes("subtitles/subtitle"))
            {
                try
                {
                    string start = node.Attributes["start"].InnerText;
                    string stop = node.Attributes["stop"].InnerText;
                    string text = node.SelectSingleNode("text").InnerText;

                    subtitle.Paragraphs.Add(new Paragraph(DecodeTimeCode(start), DecodeTimeCode(stop), text));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                    _errorCount++;
                }
            }
            subtitle.Renumber(1);
        }

    }
}
