﻿using Nikse.SubtitleEdit.Core;
using Nikse.SubtitleEdit.Forms;
using Nikse.SubtitleEdit.Logic.SubtitleFormats;
using Nikse.SubtitleEdit.Logic.VideoFormats.Matroska;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Nikse.SubtitleEdit.Logic
{
    public static class CommandLineConvert
    {
        public static void Convert(string title, string[] args) // E.g.: /convert *.txt SubRip
        {
            const int ATTACH_PARENT_PROCESS = -1;
            if (!Configuration.IsRunningOnMac() && !Configuration.IsRunningOnLinux())
                NativeMethods.AttachConsole(ATTACH_PARENT_PROCESS);

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine(title + " - Batch converter");
            Console.WriteLine();
            Console.WriteLine("- Syntax: SubtitleEdit /convert <pattern> <name-of-format-without-spaces> [/offset:hh:mm:ss:ms] [/encoding:<encoding name>] [/fps:<frame rate>] [/targetfps:<frame rate>] [/inputfolder:<input folder>] [/outputfolder:<output folder>] [/pac-codepage:<code page>]");
            Console.WriteLine();
            Console.WriteLine("    example: SubtitleEdit /convert *.srt sami");
            Console.WriteLine("    list available formats: SubtitleEdit /convert /list");
            Console.WriteLine();

            string currentDir = Directory.GetCurrentDirectory();

            if (args.Length < 4)
            {
                if (args.Length == 3 && (args[2].Equals("/list", StringComparison.OrdinalIgnoreCase) || args[2].Equals("-list", StringComparison.OrdinalIgnoreCase)))
                {
                    Console.WriteLine("- Supported formats (input/output):");
                    foreach (SubtitleFormat format in SubtitleFormat.AllSubtitleFormats)
                    {
                        Console.WriteLine("    " + format.Name.Replace(" ", string.Empty));
                    }
                    Console.WriteLine();
                    Console.WriteLine("- Supported formats (input only):");
                    Console.WriteLine("    " + new CapMakerPlus().FriendlyName);
                    Console.WriteLine("    " + new Captionate().FriendlyName);
                    Console.WriteLine("    " + new Cavena890().FriendlyName);
                    Console.WriteLine("    " + new CheetahCaption().FriendlyName);
                    Console.WriteLine("    " + new Chk().FriendlyName);
                    Console.WriteLine("    Matroska (.mkv)");
                    Console.WriteLine("    Matroska subtitle (.mks)");
                    Console.WriteLine("    " + new NciCaption().FriendlyName);
                    Console.WriteLine("    " + new AvidStl().FriendlyName);
                    Console.WriteLine("    " + new Pac().FriendlyName);
                    Console.WriteLine("    " + new Spt().FriendlyName);
                    Console.WriteLine("    " + new Ultech130().FriendlyName);
                }

                Console.WriteLine();
                Console.Write(currentDir + ">");
                if (!Configuration.IsRunningOnMac() && !Configuration.IsRunningOnLinux())
                    NativeMethods.FreeConsole();
                Environment.Exit(1);
            }

            int count = 0;
            int converted = 0;
            int errors = 0;
            try
            {
                int max = args.Length;

                string pattern = args[2];
                string toFormat = args[3];
                string offset = string.Empty;
                for (int idx = 4; idx < max; idx++)
                    if (args.Length > idx && args[idx].StartsWith("/offset:", StringComparison.OrdinalIgnoreCase))
                        offset = args[idx].ToLower();

                string fps = string.Empty;
                for (int idx = 4; idx < max; idx++)
                    if (args.Length > idx && args[idx].StartsWith("/fps:", StringComparison.OrdinalIgnoreCase))
                        fps = args[idx].ToLower();
                if (fps.Length > 6)
                {
                    fps = fps.Remove(0, 5).Replace(",", ".").Trim();
                    double d;
                    if (double.TryParse(fps, System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.CultureInfo.InvariantCulture, out d))
                    {
                        Configuration.Settings.General.CurrentFrameRate = d;
                    }
                }

                string targetFps = string.Empty;
                double? targetFrameRate = null;
                for (int idx = 4; idx < max; idx++)
                    if (args.Length > idx && args[idx].StartsWith("/targetfps:", StringComparison.OrdinalIgnoreCase))
                        targetFps = args[idx].ToLower();
                if (targetFps.Length > 12)
                {
                    targetFps = targetFps.Remove(0, 11).Replace(",", ".").Trim();
                    double d;
                    if (double.TryParse(targetFps, System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.CultureInfo.InvariantCulture, out d))
                    {
                        targetFrameRate = d;
                    }
                }

                string targetEncodingName = string.Empty;
                for (int idx = 4; idx < max; idx++)
                    if (args.Length > idx && args[idx].StartsWith("/encoding:", StringComparison.OrdinalIgnoreCase))
                        targetEncodingName = args[idx].ToLower();
                var targetEncoding = Encoding.UTF8;
                try
                {
                    if (!string.IsNullOrEmpty(targetEncodingName))
                    {
                        targetEncodingName = targetEncodingName.Substring(10);
                        if (!string.IsNullOrEmpty(targetEncodingName))
                            targetEncoding = Encoding.GetEncoding(targetEncodingName);
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine("Unable to set encoding (" + exception.Message + ") - using UTF-8");
                    targetEncoding = Encoding.UTF8;
                }

                string outputFolder = string.Empty;
                for (int idx = 4; idx < max; idx++)
                    if (args.Length > idx && args[idx].StartsWith("/outputfolder:", StringComparison.OrdinalIgnoreCase))
                        outputFolder = args[idx].ToLower();
                if (outputFolder.Length > "/outputFolder:".Length)
                {
                    outputFolder = outputFolder.Remove(0, "/outputFolder:".Length);
                    if (!Directory.Exists(outputFolder))
                        outputFolder = string.Empty;
                }

                string inputFolder = Directory.GetCurrentDirectory();
                for (int idx = 4; idx < max; idx++)
                    if (args.Length > idx && args[idx].StartsWith("/inputFolder:", StringComparison.OrdinalIgnoreCase))
                        inputFolder = args[idx].ToLower();
                if (inputFolder.Length > "/inputFolder:".Length)
                {
                    inputFolder = inputFolder.Remove(0, "/inputFolder:".Length);
                    if (!Directory.Exists(inputFolder))
                        inputFolder = Directory.GetCurrentDirectory();
                }

                string pacCodePage = string.Empty;
                for (int idx = 4; idx < max; idx++)
                    if (args.Length > idx && args[idx].StartsWith("/pac-codepage:", StringComparison.OrdinalIgnoreCase))
                        pacCodePage = args[idx].ToLower();
                if (pacCodePage.Length > "/pac-codepage:".Length)
                    pacCodePage = pacCodePage.Remove(0, "/pac-codepage:".Length);

                bool overwrite = false;
                for (int idx = 4; idx < max; idx++)
                    if (args.Length > idx && args[idx].Equals("/overwrite", StringComparison.OrdinalIgnoreCase))
                        overwrite = true;

                string[] files;
                string inputDirectory = Directory.GetCurrentDirectory();
                if (!string.IsNullOrEmpty(inputFolder))
                    inputDirectory = inputFolder;

                if (pattern.Contains(',') && !File.Exists(pattern))
                {
                    files = pattern.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int k = 0; k < files.Length; k++)
                        files[k] = files[k].Trim();
                }
                else
                {
                    int indexOfDirectorySeparatorChar = pattern.LastIndexOf(Path.DirectorySeparatorChar);
                    if (indexOfDirectorySeparatorChar > 0 && indexOfDirectorySeparatorChar < pattern.Length)
                    {
                        pattern = pattern.Substring(indexOfDirectorySeparatorChar + 1);
                        inputDirectory = args[2].Substring(0, indexOfDirectorySeparatorChar);
                    }
                    files = Directory.GetFiles(inputDirectory, pattern);
                }

                var formats = SubtitleFormat.AllSubtitleFormats;
                foreach (string fName in files)
                {
                    string fileName = fName;
                    count++;

                    if (!string.IsNullOrEmpty(inputFolder) && File.Exists(Path.Combine(inputFolder, fileName)))
                        fileName = Path.Combine(inputFolder, fileName);

                    if (File.Exists(fileName))
                    {
                        var sub = new Subtitle();
                        SubtitleFormat format = null;
                        bool done = false;

                        if (Path.GetExtension(fileName).Equals(".mkv", StringComparison.OrdinalIgnoreCase) || Path.GetExtension(fileName).Equals(".mks", StringComparison.OrdinalIgnoreCase))
                        {
                            using (var matroska = new MatroskaFile(fileName))
                            {
                                if (matroska.IsValid)
                                {
                                    var tracks = matroska.GetTracks();
                                    if (tracks.Count > 0)
                                    {
                                        foreach (var track in tracks)
                                        {
                                            if (track.CodecId.Equals("S_VOBSUB", StringComparison.OrdinalIgnoreCase))
                                            {
                                                Console.WriteLine("{0}: {1} - Cannot convert from VobSub image based format!", fileName, toFormat);
                                            }
                                            else if (track.CodecId.Equals("S_HDMV/PGS", StringComparison.OrdinalIgnoreCase))
                                            {
                                                Console.WriteLine("{0}: {1} - Cannot convert from Blu-ray image based format!", fileName, toFormat);
                                            }
                                            else
                                            {
                                                var ss = matroska.GetSubtitle(track.TrackNumber, null);
                                                format = Utilities.LoadMatroskaTextSubtitle(track, matroska, ss, sub);
                                                string newFileName = fileName;
                                                if (tracks.Count > 1)
                                                    newFileName = fileName.Insert(fileName.Length - 4, "_" + track.TrackNumber + "_" + track.Language.Replace("?", string.Empty).Replace("!", string.Empty).Replace("*", string.Empty).Replace(",", string.Empty).Replace("/", string.Empty).Trim());

                                                if (format.GetType() == typeof(AdvancedSubStationAlpha) || format.GetType() == typeof(SubStationAlpha))
                                                {
                                                    if (toFormat.ToLower() != new AdvancedSubStationAlpha().Name.ToLower().Replace(" ", string.Empty) &&
                                                        toFormat.ToLower() != new SubStationAlpha().Name.ToLower().Replace(" ", string.Empty))
                                                    {

                                                        foreach (SubtitleFormat sf in formats)
                                                        {
                                                            if (sf.Name.Replace(" ", string.Empty).Equals(toFormat, StringComparison.OrdinalIgnoreCase) || sf.Name.Replace(" ", string.Empty).Equals(toFormat.Replace(" ", string.Empty), StringComparison.OrdinalIgnoreCase))
                                                            {
                                                                format.RemoveNativeFormatting(sub, sf);
                                                                break;
                                                            }
                                                        }

                                                    }
                                                }

                                                BatchConvertSave(toFormat, offset, targetEncoding, outputFolder, count, ref converted, ref errors, formats, newFileName, sub, format, overwrite, pacCodePage, targetFrameRate);
                                                done = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        var fi = new FileInfo(fileName);
                        if (fi.Length < 10 * 1024 * 1024 && !done) // max 10 mb
                        {
                            Encoding encoding;
                            format = sub.LoadSubtitle(fileName, out encoding, null, true);

                            if (format == null || format.GetType() == typeof(Ebu))
                            {
                                var ebu = new Ebu();
                                if (ebu.IsMine(null, fileName))
                                {
                                    ebu.LoadSubtitle(sub, null, fileName);
                                    format = ebu;
                                }
                            }
                            if (format == null)
                            {
                                var pac = new Pac();
                                if (pac.IsMine(null, fileName))
                                {
                                    pac.BatchMode = true;
                                    pac.LoadSubtitle(sub, null, fileName);
                                    format = pac;
                                }
                            }
                            if (format == null)
                            {
                                var cavena890 = new Cavena890();
                                if (cavena890.IsMine(null, fileName))
                                {
                                    cavena890.LoadSubtitle(sub, null, fileName);
                                    format = cavena890;
                                }
                            }
                            if (format == null)
                            {
                                var spt = new Spt();
                                if (spt.IsMine(null, fileName))
                                {
                                    spt.LoadSubtitle(sub, null, fileName);
                                    format = spt;
                                }
                            }
                            if (format == null)
                            {
                                var cheetahCaption = new CheetahCaption();
                                if (cheetahCaption.IsMine(null, fileName))
                                {
                                    cheetahCaption.LoadSubtitle(sub, null, fileName);
                                    format = cheetahCaption;
                                }
                            }
                            if (format == null)
                            {
                                var chk = new Chk();
                                if (chk.IsMine(null, fileName))
                                {
                                    chk.LoadSubtitle(sub, null, fileName);
                                    format = chk;
                                }
                            }
                            if (format == null)
                            {
                                var ayato = new Ayato();
                                if (ayato.IsMine(null, fileName))
                                {
                                    ayato.LoadSubtitle(sub, null, fileName);
                                    format = ayato;
                                }
                            }
                            if (format == null)
                            {
                                var capMakerPlus = new CapMakerPlus();
                                if (capMakerPlus.IsMine(null, fileName))
                                {
                                    capMakerPlus.LoadSubtitle(sub, null, fileName);
                                    format = capMakerPlus;
                                }
                            }
                            if (format == null)
                            {
                                var captionate = new Captionate();
                                if (captionate.IsMine(null, fileName))
                                {
                                    captionate.LoadSubtitle(sub, null, fileName);
                                    format = captionate;
                                }
                            }
                            if (format == null)
                            {
                                var ultech130 = new Ultech130();
                                if (ultech130.IsMine(null, fileName))
                                {
                                    ultech130.LoadSubtitle(sub, null, fileName);
                                    format = ultech130;
                                }
                            }
                            if (format == null)
                            {
                                var nciCaption = new NciCaption();
                                if (nciCaption.IsMine(null, fileName))
                                {
                                    nciCaption.LoadSubtitle(sub, null, fileName);
                                    format = nciCaption;
                                }
                            }
                            if (format == null)
                            {
                                var tsb4 = new TSB4();
                                if (tsb4.IsMine(null, fileName))
                                {
                                    tsb4.LoadSubtitle(sub, null, fileName);
                                    format = tsb4;
                                }
                            }
                            if (format == null)
                            {
                                var avidStl = new AvidStl();
                                if (avidStl.IsMine(null, fileName))
                                {
                                    avidStl.LoadSubtitle(sub, null, fileName);
                                    format = avidStl;
                                }
                            }
                            if (format == null)
                            {
                                var elr = new ELRStudioClosedCaption();
                                if (elr.IsMine(null, fileName))
                                {
                                    elr.LoadSubtitle(sub, null, fileName);
                                    format = elr;
                                }
                            }
                        }

                        if (format == null)
                        {
                            if (fi.Length < 1024 * 1024) // max 1 mb
                                Console.WriteLine("{0}: {1} - input file format unknown!", fileName, toFormat);
                            else
                                Console.WriteLine("{0}: {1} - input file too large!", fileName, toFormat);
                        }
                        else if (!done)
                        {
                            BatchConvertSave(toFormat, offset, targetEncoding, outputFolder, count, ref converted, ref errors, formats, fileName, sub, format, overwrite, pacCodePage, targetFrameRate);
                        }
                    }
                    else
                    {
                        Console.WriteLine("{0}: {1} - file not found!", count, fileName);
                        errors++;
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine();
                Console.WriteLine("Ups - an error occured: " + exception.Message);
                Console.WriteLine();
            }

            Console.WriteLine();
            Console.WriteLine("{0} file(s) converted", converted);
            Console.WriteLine();
            Console.Write(currentDir + ">");

            if (!Configuration.IsRunningOnMac() && !Configuration.IsRunningOnLinux())
                NativeMethods.FreeConsole();

            if (count == converted && errors == 0)
                Environment.Exit(0);
            else
                Environment.Exit(1);
        }

        internal static bool BatchConvertSave(string toFormat, string offset, Encoding targetEncoding, string outputFolder, int count, ref int converted, ref int errors, IList<SubtitleFormat> formats, string fileName, Subtitle sub, SubtitleFormat format, bool overwrite, string pacCodePage, double? targetFrameRate)
        {
            double oldFrameRate = Configuration.Settings.General.CurrentFrameRate;

            try
            {

                // adjust offset
                if (!string.IsNullOrEmpty(offset) && (offset.StartsWith("/offset:") || offset.StartsWith("offset:")))
                {
                    string[] parts = offset.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 5)
                    {
                        try
                        {
                            var ts = new TimeSpan(0, int.Parse(parts[1].TrimStart('-')), int.Parse(parts[2]), int.Parse(parts[3]), int.Parse(parts[4]));
                            if (parts[1].StartsWith('-'))
                                sub.AddTimeToAllParagraphs(ts.Negate());
                            else
                                sub.AddTimeToAllParagraphs(ts);
                        }
                        catch
                        {
                            Console.Write(" (unable to read offset " + offset + ")");
                        }
                    }
                }

                // adjust frame rate
                if (targetFrameRate.HasValue)
                {
                    sub.ChangeFrameRate(Configuration.Settings.General.CurrentFrameRate, targetFrameRate.Value);
                    Configuration.Settings.General.CurrentFrameRate = targetFrameRate.Value;
                }

                bool targetFormatFound = false;
                string outputFileName;
                foreach (SubtitleFormat sf in formats)
                {
                    if (sf.IsTextBased && (sf.Name.Replace(" ", string.Empty).Equals(toFormat, StringComparison.OrdinalIgnoreCase) || sf.Name.Replace(" ", string.Empty).Equals(toFormat.Replace(" ", string.Empty), StringComparison.OrdinalIgnoreCase)))
                    {
                        targetFormatFound = true;
                        sf.BatchMode = true;
                        outputFileName = FormatOutputFileNameForBatchConvert(fileName, sf.Extension, outputFolder, overwrite);
                        Console.Write("{0}: {1} -> {2}...", count, Path.GetFileName(fileName), outputFileName);
                        if (sf.IsFrameBased && !sub.WasLoadedWithFrameNumbers)
                            sub.CalculateFrameNumbersFromTimeCodesNoCheck(Configuration.Settings.General.CurrentFrameRate);
                        else if (sf.IsTimeBased && sub.WasLoadedWithFrameNumbers)
                            sub.CalculateTimeCodesFromFrameNumbers(Configuration.Settings.General.CurrentFrameRate);

                        if (sf.GetType() == typeof(ItunesTimedText) || sf.GetType() == typeof(ScenaristClosedCaptions) || sf.GetType() == typeof(ScenaristClosedCaptionsDropFrame))
                        {
                            Encoding outputEnc = new UTF8Encoding(false); // create encoding with no BOM
                            using (var file = new StreamWriter(outputFileName, false, outputEnc)) // open file with encoding
                            {
                                file.Write(sub.ToText(sf));
                            } // save and close it
                        }
                        else if (targetEncoding == Encoding.UTF8 && (format.GetType() == typeof(TmpegEncAW5) || format.GetType() == typeof(TmpegEncXml)))
                        {
                            Encoding outputEnc = new UTF8Encoding(false); // create encoding with no BOM
                            using (var file = new StreamWriter(outputFileName, false, outputEnc)) // open file with encoding
                            {
                                file.Write(sub.ToText(sf));
                            } // save and close it
                        }
                        else
                        {
                            File.WriteAllText(outputFileName, sub.ToText(sf), targetEncoding);
                        }

                        if (format.GetType() == typeof(Sami) || format.GetType() == typeof(SamiModern))
                        {
                            var sami = (Sami)format;
                            foreach (string className in Sami.GetStylesFromHeader(sub.Header))
                            {
                                var newSub = new Subtitle();
                                foreach (Paragraph p in sub.Paragraphs)
                                {
                                    if (p.Extra != null && p.Extra.Trim().Equals(className.Trim(), StringComparison.OrdinalIgnoreCase))
                                        newSub.Paragraphs.Add(p);
                                }
                                if (newSub.Paragraphs.Count > 0 && newSub.Paragraphs.Count < sub.Paragraphs.Count)
                                {
                                    string s = fileName;
                                    if (s.LastIndexOf('.') > 0)
                                        s = s.Insert(s.LastIndexOf('.'), "_" + className);
                                    else
                                        s += "_" + className + format.Extension;
                                    outputFileName = FormatOutputFileNameForBatchConvert(s, sf.Extension, outputFolder, overwrite);
                                    File.WriteAllText(outputFileName, newSub.ToText(sf), targetEncoding);
                                }
                            }
                        }
                        Console.WriteLine(" done.");
                    }
                }
                if (!targetFormatFound)
                {
                    var ebu = new Ebu();
                    if (ebu.Name.Replace(" ", string.Empty).Equals(toFormat.Replace(" ", string.Empty), StringComparison.OrdinalIgnoreCase))
                    {
                        targetFormatFound = true;
                        outputFileName = FormatOutputFileNameForBatchConvert(fileName, ebu.Extension, outputFolder, overwrite);
                        Console.Write("{0}: {1} -> {2}...", count, Path.GetFileName(fileName), outputFileName);
                        Ebu.Save(outputFileName, sub, true);
                        Console.WriteLine(" done.");
                    }
                }
                if (!targetFormatFound)
                {
                    var pac = new Pac();
                    if (pac.Name.Replace(" ", string.Empty).Equals(toFormat, StringComparison.OrdinalIgnoreCase) || toFormat.Equals("pac", StringComparison.OrdinalIgnoreCase) || toFormat.Equals(".pac", StringComparison.OrdinalIgnoreCase))
                    {
                        pac.BatchMode = true;
                        int codePage;
                        if (!string.IsNullOrEmpty(pacCodePage) && int.TryParse(pacCodePage, out codePage))
                            pac.CodePage = codePage;
                        targetFormatFound = true;
                        outputFileName = FormatOutputFileNameForBatchConvert(fileName, pac.Extension, outputFolder, overwrite);
                        Console.Write("{0}: {1} -> {2}...", count, Path.GetFileName(fileName), outputFileName);
                        pac.Save(outputFileName, sub);
                        Console.WriteLine(" done.");
                    }
                }
                if (!targetFormatFound)
                {
                    var cavena890 = new Cavena890();
                    if (cavena890.Name.Replace(" ", string.Empty).Equals(toFormat, StringComparison.OrdinalIgnoreCase))
                    {
                        targetFormatFound = true;
                        outputFileName = FormatOutputFileNameForBatchConvert(fileName, cavena890.Extension, outputFolder, overwrite);
                        Console.Write("{0}: {1} -> {2}...", count, Path.GetFileName(fileName), outputFileName);
                        cavena890.Save(outputFileName, sub);
                        Console.WriteLine(" done.");
                    }
                }
                if (!targetFormatFound)
                {
                    var cheetahCaption = new CheetahCaption();
                    if (cheetahCaption.Name.Replace(" ", string.Empty).Equals(toFormat, StringComparison.OrdinalIgnoreCase))
                    {
                        targetFormatFound = true;
                        outputFileName = FormatOutputFileNameForBatchConvert(fileName, cheetahCaption.Extension, outputFolder, overwrite);
                        Console.Write("{0}: {1} -> {2}...", count, Path.GetFileName(fileName), outputFileName);
                        CheetahCaption.Save(outputFileName, sub);
                        Console.WriteLine(" done.");
                    }
                }
                if (!targetFormatFound)
                {
                    var capMakerPlus = new CapMakerPlus();
                    if (capMakerPlus.Name.Replace(" ", string.Empty).Equals(toFormat, StringComparison.OrdinalIgnoreCase))
                    {
                        targetFormatFound = true;
                        outputFileName = FormatOutputFileNameForBatchConvert(fileName, capMakerPlus.Extension, outputFolder, overwrite);
                        Console.Write("{0}: {1} -> {2}...", count, Path.GetFileName(fileName), outputFileName);
                        CapMakerPlus.Save(outputFileName, sub);
                        Console.WriteLine(" done.");
                    }
                }
                if (!targetFormatFound)
                {
                    if (Configuration.Settings.Language.BatchConvert.PlainText == toFormat || Configuration.Settings.Language.BatchConvert.PlainText.Replace(" ", string.Empty).Equals(toFormat.Replace(" ", string.Empty), StringComparison.OrdinalIgnoreCase))
                    {
                        targetFormatFound = true;
                        outputFileName = FormatOutputFileNameForBatchConvert(fileName, ".txt", outputFolder, overwrite);
                        Console.Write("{0}: {1} -> {2}...", count, Path.GetFileName(fileName), outputFileName);
                        File.WriteAllText(outputFileName, ExportText.GeneratePlainText(sub, false, false, false, false, false, false, string.Empty, true, false, true, true, false), targetEncoding);
                        Console.WriteLine(" done.");
                    }
                }
                if (!targetFormatFound)
                {
                    Console.WriteLine("{0}: {1} - target format '{2}' not found!", count, fileName, toFormat);
                    errors++;
                    return false;
                }
                converted++;
                return true;
            }
            finally
            {
                Configuration.Settings.General.CurrentFrameRate = oldFrameRate;
            }
        }

        private static string FormatOutputFileNameForBatchConvert(string fileName, string extension, string outputFolder, bool overwrite)
        {
            string outputFileName = Path.ChangeExtension(fileName, extension);
            if (!string.IsNullOrEmpty(outputFolder))
                outputFileName = Path.Combine(outputFolder, Path.GetFileName(outputFileName));
            if (File.Exists(outputFileName) && !overwrite)
                outputFileName = Path.ChangeExtension(outputFileName, Guid.NewGuid() + extension);
            return outputFileName;
        }

    }
}