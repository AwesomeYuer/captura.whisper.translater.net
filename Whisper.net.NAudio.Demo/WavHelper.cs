// Licensed under the MIT license: https://opensource.org/licenses/MIT

namespace Whisper.net.Demo;

/// <summary>
/// 生成wav文件的帮助类,本类来自网络，作者不详
/// </summary>
public static class WavHelper
{

    public static Stream WriteStream(byte[] data, int sampleRate, short bitsPerSample, short channels)
    {

        //SamplesPerSecond = sampleRate;
        //BitsPerSample = BitsPerSample;
        //Channels = channels;
        var BlockAlign = (short)(channels * (bitsPerSample / 8));
        var AverageBytesPerSecond = BlockAlign * sampleRate;

        /**************************************************************************  
           Here is where the file will be created. A  
           wave file is a RIFF file, which has chunks  
           of data that describe what the file contains.  
           A wave RIFF file is put together like this:  
           The 12 byte RIFF chunk is constructed like this:  
           Bytes 0 - 3 :  'R' 'I' 'F' 'F'  
           Bytes 4 - 7 :  Length of file, minus the first 8 bytes of the RIFF description.  
                             (4 bytes for "WAVE" + 24 bytes for format chunk length +  
                             8 bytes for data chunk description + actual sample data size.)  
            Bytes 8 - 11: 'W' 'A' 'V' 'E'  
            The 24 byte FORMAT chunk is constructed like this:  
            Bytes 0 - 3 : 'f' 'm' 't' ' '  
            Bytes 4 - 7 : The format chunk length. This is always 16.  
            Bytes 8 - 9 : File padding. Always 1.  
            Bytes 10- 11: Number of channels. Either 1 for mono,  or 2 for stereo.  
            Bytes 12- 15: Sample rate.  
            Bytes 16- 19: Number of bytes per second.  
            Bytes 20- 21: Bytes per sample. 1 for 8 bit mono, 2 for 8 bit stereo or  16 bit mono, 4 for 16 bit stereo.  
            Bytes 22- 23: Number of bits per sample.  
            The DATA chunk is constructed like this:  
            Bytes 0 - 3 : 'd' 'a' 't' 'a'  
            Bytes 4 - 7 : Length of data, in bytes.  
            Bytes 8 -: Actual sample data.  
          ***************************************************************************/
        // Set up file with RIFF chunk info.  
        char[] chunkRiff = { 'R', 'I', 'F', 'F' };
        char[] chunkType = { 'W', 'A', 'V', 'E' };
        char[] chunkFmt = { 'f', 'm', 't', ' ' };
        char[] chunkData = { 'd', 'a', 't', 'a' };

        short padding = 1;                // File padding  
        var formatChunkLength = 0x10;  // Format chunk length.  
        var length = 0;                // File length, minus first 8 bytes of RIFF description. This will be filled in later.
        short bytesPerSample = 0;     // Bytes per sample.  

        // 一个样本点的字节数目  
        if
            (8 == bitsPerSample && 1 == channels)
        {
            bytesPerSample = 1;
        }
        else if
            (8 == bitsPerSample && 2 == channels || 16 == bitsPerSample && 1 == channels)
        {
            bytesPerSample = 2;
        }
        else if
            (16 == bitsPerSample && 2 == channels)
        {
            bytesPerSample = 4;
        }

        using var stream = new MemoryStream();

        using var binaryWriter = new BinaryWriter(stream);
        // RIFF 块  
        binaryWriter.Write(chunkRiff);
        binaryWriter.Write(length);
        binaryWriter.Write(chunkType);

        // WAVE块  
        binaryWriter.Write(chunkFmt);
        binaryWriter.Write(formatChunkLength);
        binaryWriter.Write(padding);
        binaryWriter.Write(channels);
        binaryWriter.Write(sampleRate);
        binaryWriter.Write(AverageBytesPerSecond);
        binaryWriter.Write(bytesPerSample);
        binaryWriter.Write(bitsPerSample);

        // 数据块  
        binaryWriter.Write(chunkData);
        binaryWriter.Write(0);   // The sample length will be written in later.

        binaryWriter.Write(data, 0, data.Length);
        var samplesCount = data.Length;

        // 写WAV文件尾  
        binaryWriter.Seek(4, SeekOrigin.Begin);
        binaryWriter.Write(samplesCount + 36);
        binaryWriter.Seek(40, SeekOrigin.Begin);
        binaryWriter.Write(samplesCount);

        var copyOfStream = new MemoryStream();
        stream.Position = 0;
        stream.CopyTo(copyOfStream);

        binaryWriter.Close();

        return copyOfStream;
    }
}
