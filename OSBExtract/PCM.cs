using System;
using System.IO;
using System.Text;

class PCM
{
    public static void ToWav(byte[] dataBuffer, int dataLength, string outFile, int formatSize = 16, Int16 formatCode = 1,
         Int16 channels = 1, int sampleRate = 44100, Int16 bitDepth = 16)
    {
        //Init the Wave Header byte Array
        byte[] WavHeader = new byte[44];

        //Encode our ASCII
        byte[] RIFF = Encoding.ASCII.GetBytes("RIFF");
        byte[] WAVE = Encoding.ASCII.GetBytes("WAVE");
        byte[] fmt = Encoding.ASCII.GetBytes("fmt ");
        byte[] data = Encoding.ASCII.GetBytes("data");

        //Get Overall File Size
        byte[] bLength = BitConverter.GetBytes(dataLength + WavHeader.Length - 8);
        //Get Format Size
        byte[] bFormatSize = BitConverter.GetBytes(formatSize);
        //Get Format Code
        byte[] bFormatCode = BitConverter.GetBytes(formatCode);
        //Get Channel Count
        byte[] bChannels = BitConverter.GetBytes(channels);
        //Get Sample Rate
        byte[] bSampleRate = BitConverter.GetBytes(sampleRate);
        //Get Bytes Per Second
        byte[] bByteRate = BitConverter.GetBytes((sampleRate * bitDepth * channels) / 8);
        //Get Data Alignment
        byte[] bDataAlign = BitConverter.GetBytes((UInt16)((bitDepth * channels) / 8));
        //Get Bit Depth
        byte[] bBitDepth = BitConverter.GetBytes(bitDepth);
        //Get Length of Data
        byte[] bDataLength = BitConverter.GetBytes(dataLength);

        //Combine above into our header
        int dstOffset = 0;
        byte[][] WavHeaderData = { RIFF, bLength, WAVE, fmt, bFormatSize, bFormatCode, bChannels, bSampleRate, bByteRate, bDataAlign, bBitDepth, data, bDataLength };

        foreach (byte[] block in WavHeaderData)
        {
            Buffer.BlockCopy(block, 0, WavHeader, dstOffset, block.Length);
            dstOffset += block.Length;
        }

        using (FileStream outStream = File.OpenWrite(outFile))
        {
            outStream.Write(WavHeader, 0, WavHeader.Length);
            outStream.Write(dataBuffer, 0, dataBuffer.Length);
        }
    }

}
