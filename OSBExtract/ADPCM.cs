using System;

// Dreamcast AICA ADPCM
// Much of this code comes from vgmstream

public static class ADPCM
{
    // fixed point (.8) amount to scale the current step size by
    // part of the same series as used in MS ADPCM "ADPCMTable"
    static int[] scaleStep =
    {
        230, 230, 230, 230, 307, 409, 512, 614,
        230, 230, 230, 230, 307, 409, 512, 614
    };

    // expand an unsigned four bit delta to a wider signed range
    static int[] scaleDelta =
    {
          1,  3,  5,  7,  9, 11, 13, 15,
         -1, -3, -5, -7, -9,-11,-13,-15
    };

    public static byte[] ToRaw(byte[] inBuffer, int startOffset, int length)
    {
        byte[] outBuffer = new byte[length * 4];

        int numSamples = length * 2;
        int stepSize = 0x7F;
        int history = 0;

        for (int i = 0, sampleCount = 0; i < numSamples; i++, sampleCount++)
        {
            int sampleNibble = (inBuffer[startOffset + (i / 2)] >> ((i & 1) == 1 ? 4 : 0)) & 0xF;

            int sampleDelta = stepSize * scaleDelta[sampleNibble];
            int newSample = history + (sampleDelta / 8);
            short newSampleClamped = Clamp16(newSample);

            BitConverter.GetBytes(newSampleClamped).CopyTo(outBuffer, sampleCount * 2);

            history = newSampleClamped;

            stepSize = (stepSize * scaleStep[sampleNibble]) / 0x100;
            if (stepSize < 0x7F) stepSize = 0x7F;
            if (stepSize > 0x6000) stepSize = 0x6000;
        }

        return outBuffer;
    }

    private static short Clamp16(int i)
    {
        return (short)((i < -32768) ? -32768 : ((i > 32767) ? 32767 : i));
    }
}