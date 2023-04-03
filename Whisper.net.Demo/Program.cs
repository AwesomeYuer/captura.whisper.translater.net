// Licensed under the MIT license: https://opensource.org/licenses/MIT

//using System;
//using System.IO;
//using System.Threading;
//using System.Threading.Tasks;
using Captura.Audio;
using Captura;
using Captura.Audio;
using Captura.Video;
using CommandLine;
using NAudio.CoreAudioApi;
using Whisper.net;
using Whisper.net.Ggml;
using Whisper.net.Wave;

IAudioSource audioSource = new NAudioSource();

IAudioItem microphone, speaker = null!;

var microphones = audioSource
                        .Microphones
                        .ToArray();
microphone = microphones[0];

var speakers = audioSource
                        .Speakers
                        .ToArray();
speaker = speakers[0];

var audioProviders =
                    new[]
                    {
                          audioSource.GetAudioProvider(microphone, speaker)
                        //, audioSource.GetAudioProvider(null, speaker)
                    }
                    .Where(M => M != null)
                    .ToArray();
var noVideoItem = new NoVideoItem(new WaveItem());

Stream stream = new MemoryStream();
var recorders =
            audioProviders
                        .Select
                            (
                                (M, Index) =>
                                {
                                    return
                                        GetAudioRecorder(null, M );
                                }
                            )
                        .ToArray();


IRecorder recorder;
if (recorders.Length > 1)
{
    recorder = new MultiRecorder(recorders);
}
else
{
    recorder = recorders[0];
}

var t = new Thread
        (
            recorder.Start
        );

        t.Start();

var input = string.Empty;

while ("q" != (input = Console.ReadLine()))
{

}
recorder.Stop();

stream.Position = 0;
var filePath = @"d:\ccc.wav";
File.Delete(filePath);
var fileStream = File.Create(filePath);

stream.CopyTo(fileStream);
fileStream.Close();
stream.Position = 0;
//return;
var options = Parser
                .Default
                .ParseArguments
                        <Options>
                    (args);
stream.Position = 0;
options.Value.InputWavStream = stream;

await options.WithParsedAsync
            (Demo);

async Task Demo(Options opt)
{
    if (!File.Exists(opt.ModelName))
    {
        Console.WriteLine($"Downloading Model {opt.ModelName}");
        using var modelStream = await WhisperGgmlDownloader.GetGgmlModelAsync(opt.ModelType);
        using var fileWriter = File.OpenWrite(opt.ModelName!);
        await modelStream.CopyToAsync(fileWriter);
    }

    switch (opt.Command)
    {
        case "lang-detect":
            LanguageIdentification(opt);
            break;
        case "transcribe":
        case "translate":
            await FullDetection(opt);
            break;
        default:
            Console.WriteLine("Unknown command");
            break;
    }
}

void LanguageIdentification(Options opt)
{
    var bufferedModel = File.ReadAllBytes(opt.ModelName!);

    // Same factory can be used by multiple task to create processors.
    using var factory = WhisperFactory.FromBuffer(bufferedModel);

    var builder = factory.CreateBuilder()
       .WithLanguage(opt.Language!);

    using var processor = builder.Build();

    //using var fileStream = opt.InputWavStream;
        //File.OpenRead(opt.FileName!)
      //  ;

    var wave = new WaveParser(opt.InputWavStream!);

    var samples = wave.GetAvgSamples();

    var language = processor.DetectLanguage(samples, speedUp: true);
    Console.WriteLine("Language is " + language);
}

async Task FullDetection(Options opt)
{
    // Same factory can be used by multiple task to create processors.
    using var factory = WhisperFactory.FromPath(opt.ModelName!);

    var builder = factory.CreateBuilder()
        .WithLanguage(opt.Language!);

    if (opt.Command == "translate")
    {
        builder.WithTranslate();
    }

    using var processor = builder.Build();

    //using var fileStream = File.OpenRead(opt.FileName!);

    await foreach (var segment in processor.ProcessAsync(opt.InputWavStream!, CancellationToken.None))
    {
        Console.WriteLine($"New Segment: {segment.Start} ==> {segment.End} : {segment.Text}");
    }
}



IRecorder GetAudioRecorder(NoVideoItem AudioWriter, IAudioProvider AudioProvider, string AudioFileName = null)
{
    var audioFileWriter = AudioWriter.AudioWriterItem.GetAudioFileWriter(
        "CurrentFileName",
        AudioProvider?.WaveFormat,
        80);

    return new AudioRecorder(audioFileWriter, AudioProvider);
}





public class Options
{
    [Option('t', "command", Required = false, HelpText = "Command to run (lang-detect, transcribe or translate)", Default = "transcribe")]
    public string? Command { get; set; }

    [Option('f', "file", Required = false, HelpText = "File to process", Default = "kennedy.wav")]
    public string? FileName { get; set; }

    [Option('l', "lang", Required = false, HelpText = "Language", Default = "auto")]
    public string? Language { get; set; }

    [Option('m', "modelFile", Required = false, HelpText = "Model to use (filename", Default = "ggml-base.bin")]
    public string? ModelName { get; set; }

    [Option('g', "ggml", Required = false, HelpText = "Ggml Model type to download (if not exists)", Default = GgmlType.Base)]
    public GgmlType ModelType { get; set; }


    public Stream? InputWavStream { get; set; }
}
