/***********
This is borrowed MIT Licensed code from Sappharad from their repo https://github.com/Sappharad/AicaADPCM2WAV

This code is used instead of the original OSBExtract code, which was based on vgmstream's ADPCM decoder.
TODO: Which vgmstream decoder did the original OSBExtract it use for reference?

***********/

public static class ADPCM
{
    #region AICA ADPCM decoding
    static readonly int[] diff_lookup = {
        1,3,5,7,9,11,13,15,
        -1,-3,-5,-7,-9,-11,-13,-15,
    };

    static int[] index_scale = {
        0x0e6, 0x0e6, 0x0e6, 0x0e6, 0x133, 0x199, 0x200, 0x266
    };

    public static byte[] adpcm2pcm(in byte[] input, uint src, in uint length)
    {
        byte[] dst = new byte[length * 4];
        int dstLoc = 0;
        int cur_quant = 0x7f;
        int cur_sample = 0;
        bool highNybble = false;

        while (dstLoc < dst.Length)
        {
            int shift1 = highNybble ? 4 : 0;
            int delta = (input[src] >> shift1) & 0xf;

            int x = cur_quant * diff_lookup[delta & 15];
            x = cur_sample + ((int)(x + ((uint)x >> 29)) >> 3);
            cur_sample = (x < -32768) ? -32768 : ((x > 32767) ? 32767 : x);
            cur_quant = (cur_quant * index_scale[delta & 7]) >> 8;
            cur_quant = (cur_quant < 0x7f) ? 0x7f : ((cur_quant > 0x6000) ? 0x6000 : cur_quant);

            dst[dstLoc++] = (byte)(cur_sample & 0xFF);
            dst[dstLoc++] = (byte)((cur_sample >> 8) & 0xFF);

            cur_sample = cur_sample * 254 / 256;

            highNybble = !highNybble;
            if (!highNybble)
            {
                src++;
            }
        }
        return dst;
    }
    #endregion
}
