using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BluraySharp;
using BluraySharp.Common;
using CommandLine;
using BluraySharp.Extension.Ssls;
using CsmStudio.ProjectManager.Compile;
using System.IO;

namespace CsmStudioCli
{
    public class Options
    {
        [Option('i', "input", Required = true, HelpText = "Paths to subtitle ass files")]
        public IEnumerable<string> SubtitleFiles { get; set; }

        [Option('l', "lang", HelpText = "Languages of subtitles")]
        public IEnumerable<string> SubtitleLangs { get; set; }

        [Option('o', "output", HelpText = "Output filename")]
        public string OutputM2ts { get; set; }

        [Option('f', "format", Default = "1080p", HelpText = "Video Format (1080p/1080i/720p/576p/576i/480p/480i)")]
        public string Format { get; set; }

        [Option('r', "rate", Default = "23.976", HelpText = "Video Framerate (23.976/24/25/29.97/50/59.94)")]
        public string FrameRate { get; set; }

        [Option('t', "intime", Default = "0:10:00.000", HelpText = "InTime Offset, format: XX:XX:XX.XXX")]
        public string InTimeOffset { get; set; }

        [Option("server", Default = ".\\BdMuxServer\\MuxRemotingServer.exe", HelpText = "Path to Mux Server executable")]
        public string MuxServerExecutable { get; set; }

        [Option("schema", Default = ".\\BdMuxServer\\ProjectSchema", HelpText = "Path to Project Schema")]
        public string SchemaDir { get; set; }

        [Option("temp", Default = ".\\Temporary", HelpText = "Path to temporary directory")]
        public string TempDir { get; set; }

        [Option("port", Default = "9920", HelpText = "Path of Mux Server")]
        public string Port { get; set; }

        [Option("cache", Default = 838860800ul, HelpText = "SppfMaxCacheSize")]
        public ulong SppfMaxCacheSize { get; set; }

    }
    public class DummyLogger : ICompilingLogger
    {
        public void Log(int level, string fmt, params object[] param) { }
        public void Log(Exception ex) { }
    }

    public class DummyReporter : ICompilingProgressReporter
    {
        public float Amount { get; set; }
        public float Progress { get; set; }
        public bool IsCanceled { get; set; }
        public void OnTaskEnd() { }
    }

    class Program
    {
        static int Main(string[] args)
        {
            return CommandLine.Parser.Default.ParseArguments<Options>(args).MapResult(
                opts => CompileProjectWithOptions(opts),
                _ => -1
                );
        }

        static BdViFormat VideoFormatFromString(string fmt)
        {
            switch (fmt)
            {
                case "1080p":
                    return BdViFormat.Vi1080p;
                case "1080i":
                    return BdViFormat.Vi1080i;
                case "720p":
                    return BdViFormat.Vi720p;
                case "576p":
                    return BdViFormat.Vi576p;
                case "576i":
                    return BdViFormat.Vi576i;
                case "480p":
                    return BdViFormat.Vi480p;
                case "480i":
                    return BdViFormat.Vi480i;
                default:
                    return BdViFormat.Unknown;
            }
        }

        static BdViFrameRate VideoFrameRateFromString(string fmt)
        {
            switch (fmt)
            {
                case "23.976":
                    return BdViFrameRate.Vi23;
                case "24":
                    return BdViFrameRate.Vi24;
                case "25":
                    return BdViFrameRate.Vi25;
                case "29.970":
                case "29.97":
                    return BdViFrameRate.Vi29;
                case "50":
                    return BdViFrameRate.Vi50;
                case "59.940":
                case "59.94":
                    return BdViFrameRate.Vi59;
                default:
                    return BdViFrameRate.Unknown;
            }
        }

        static int CompileProjectWithOptions(Options opts)
        {
            var SubtitleFilesList = opts.SubtitleFiles.ToList();
            var SubtitleLangsStringList = opts.SubtitleLangs.ToList();
            var SubtitleCount = SubtitleFilesList.Count;
            var SubtitleLangsStringCount = SubtitleLangsStringList.Count;

            // check the number of subtitle langs
            if (SubtitleLangsStringCount > 0 && SubtitleLangsStringCount != SubtitleCount)
            {
                Console.Error.WriteLine("ERROR: The number of subtitle languages specified is not equal to the number of subtitles files.");
                return -2;
            }

            // create corresponding subtitle language list
            var SubtitleLangsList = new List<BdLang>();
            if (SubtitleLangsStringCount == 0)
            {
                // if no subtitle lang is specified, use the system language {SubtitleCount} times
                var DefaultBdLang = CultureInfo.CurrentUICulture.ThreeLetterISOLanguageName.ToBdLang();
                SubtitleLangsList.AddRange(Enumerable.Repeat(DefaultBdLang, SubtitleCount));
            }
            else
            {
                foreach (string SubtitleLang in SubtitleLangsStringList)
                {
                    var SubtitleBdLang = SubtitleLang.ToBdLang();
                    if (SubtitleBdLang == BdLang.Ivl)
                    {
                        Console.Error.WriteLine($"ERROR: Subtitle Language {SubtitleLang} is invalid.");
                        return -2;
                    }
                    SubtitleLangsList.Add(SubtitleBdLang);
                }
            }

            // validate and convert video format and framerate information
            var VideoFormat = VideoFormatFromString(opts.Format);
            var VideoFrameRate = VideoFrameRateFromString(opts.FrameRate);
            if (VideoFormat == BdViFormat.Unknown)
            {
                Console.Error.WriteLine($"ERROR: Video Format {opts.Format} is invalid.");
                return -2;
            }
            if (VideoFrameRate == BdViFrameRate.Unknown)
            {
                Console.Error.WriteLine($"ERROR: Video FrameRate {opts.FrameRate} is invalid.");
                return -2;
            }

            // prepare the clip descriptor
            DocumentClipDescriptor documentClipDescriptor = new DocumentClipDescriptor(0u, TimeSpan.Parse(opts.InTimeOffset), VideoFormat, VideoFrameRate);
            EsGroup esGroup = new EsGroup { SyncOffset = TimeSpan.Zero };
            documentClipDescriptor.EsGroups.Add(esGroup);
            foreach (var subtitle in SubtitleFilesList.Zip(SubtitleLangsList, Tuple.Create))
            {
                EsTrack esTrack = new EsTrack(BdStreamCodingType.GxPresentation, subtitle.Item2);
                esGroup.Entries[esTrack] = new EsEntry(new FileInfo(subtitle.Item1));
                EsTrackDescriptor item = new EsTrackDescriptor(documentClipDescriptor.EsGroups, esTrack);
                documentClipDescriptor.Tracks.Add(item);
            }

            // create temporary directories (same as CompilerTest)
            var ProjectId = Guid.NewGuid();
            string tOutputTempDir = Path.Combine(opts.TempDir, $"BDMV.{ProjectId}");
            DirectoryInfo directoryInfo = new DirectoryInfo(tOutputTempDir);
            directoryInfo.Create();

            // create CsmStudio project compiler and compile the project
            var compiler = new ProjectCompiler(new CompilingSettings
            {
                MuxServerExeFile = new FileInfo(opts.MuxServerExecutable),
                MuxServerUri = new Uri($"tcp://localhost:{opts.Port}/MuxRemotingService"),
                SchemaDir = new DirectoryInfo(opts.SchemaDir),
                TempDir = new DirectoryInfo(opts.TempDir),
                SppfMaxCacheSize = new UIntPtr(opts.SppfMaxCacheSize)
            }, new DummyLogger());
            var task = compiler.Compile(new DummyReporter(), ProjectId, directoryInfo, documentClipDescriptor);
            task.Wait();
            var success = task.Result;

            // copy the output files to destination if successful
            if (success)
            {
                string destFileName = Path.Combine(Path.GetDirectoryName(opts.OutputM2ts), Path.GetFileNameWithoutExtension(opts.OutputM2ts) + ".clpi");
                File.Copy(Path.Combine(tOutputTempDir, "STREAM\\00000.m2ts"), opts.OutputM2ts, overwrite: true);
                File.Copy(Path.Combine(tOutputTempDir, "CLIPINF\\00000.clpi"), destFileName, overwrite: true);
                return 0;
            }
            else
            {
                return 1;
            }
        }
    }
}
