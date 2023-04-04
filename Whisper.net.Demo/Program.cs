// Licensed under the MIT license: https://opensource.org/licenses/MIT

using CommandLine;
using NAudio.Wave;
using Whisper.net;
using Whisper.net.Demo;
using Whisper.net.Ggml;
using Whisper.net.Wave;

var options = Parser
                .Default
                .ParseArguments
                        <Options>
                    (args);

var waveIn =
    new WaveInEvent()
    //new WasapiLoopbackCapture
{
   WaveFormat = new WaveFormat(16000, 16, 2)
   //, DeviceNumber = 1,
   , BufferMilliseconds = 5 * 1000
   //, CaptureState = NAudio.CoreAudioApi.CaptureState.
};
waveIn.StartRecording();

waveIn.DataAvailable += (s, e) =>
{
    var buffer =
                new ArraySegment<byte>
                            (e.Buffer, 0, e.BytesRecorded)
                        .ToArray();
    var stream = WavHelper
                        .WriteStream
                                (
                                    buffer.ToArray()
                                    , waveIn.WaveFormat.SampleRate
                                    , (short) waveIn.WaveFormat.BitsPerSample
                                    , (short) waveIn.WaveFormat.Channels
                                );
    options.Value.Command = "translate";
    options.Value.InputWavStream = stream;
    Demo(options.Value).Wait();
};

var input = string.Empty;
while ("q" != (input = Console.ReadLine()))
{
    waveIn.StopRecording();
    waveIn.Dispose();
}
Console.ReadLine();

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
    opt.FileName = @"d:\eeee.wav";
    using var fileStream = //opt.InputWavStream;
    File.OpenRead(opt.FileName!)
      ;

    var wave = new WaveParser(opt.InputWavStream!);

    var samples = wave.GetAvgSamples();

    var language = processor.DetectLanguage(samples, speedUp: true);
    Console.WriteLine("Language is " + language);
}

async Task FullDetection(Options opt)
{
    // Same factory can be used by multiple task to create processors.
    using var factory = WhisperFactory.FromPath(opt.ModelName!);

    var builder = factory
                    .CreateBuilder()
                    .WithLanguage
                        (
                            opt.Language!
                            //"zh"
                        );

    if (opt.Command == "translate")
    {
        builder.WithTranslate();
    }

    using var processor = builder.Build();

    opt.InputWavStream!.Position = 0;

    var segments = processor.ProcessAsync(opt.InputWavStream, CancellationToken.None);
    await foreach (var segment in segments)
    {
        Console.WriteLine($"New Segment: {segment.Start} ==> {segment.End} : {segment.Text}");
    }
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

    public byte[]? InputWavBuffer { get; set; }
    public Stream? InputWavStream { get; set; }
}
