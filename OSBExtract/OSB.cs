using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

public static class OSB
{
    public class fileIndex
    {
        string _strHeader;
        byte[] _fileOffByte;
        int _fileOff;
        int _fileLen;
        bool _adpcm;
        byte[] _unknown;
        string _strFooter;

        public fileIndex(string strHeader, byte[] fileOffByte, int fileOff, int fileLen, bool adpcm, byte[] unknown, string strFooter)
        {
            this._strHeader = strHeader;
            this._fileOffByte = fileOffByte;
            this._fileOff = fileOff;
            this._fileLen = fileLen;
            this._adpcm = adpcm;
            this._unknown = unknown;
            this._strFooter = strFooter;
        }

        public string StrHeader { get { return _strHeader; } }
        public byte[] FileOffBytes { get { return _fileOffByte; } }
        public int FileOff { get { return _fileOff; } set { _fileOff = value; } }
        public int FileLen { get { return _fileLen; } set { _fileLen = value; } }
        public bool ADPCM { get { return _adpcm; } }
        public byte[] Unknown { get { return _unknown; } }
        public string StrFooter { get { return _strFooter; } }

    }

    public static byte[] ReadCustomBytes(byte[] bytes1, byte[] bytes2)
    {
        byte[] combined = new byte[bytes2.Length + bytes1.Length];
        Buffer.BlockCopy(bytes2, 0, combined, 0, bytes2.Length);
        Buffer.BlockCopy(bytes1, 0, combined, bytes2.Length, bytes1.Length);
        return combined;
    }

    public static void Extract(long starPos, string inFile, string outDir, in OSBExtract.OSBExtract.Options opts )
    {
        //If we want verbose output don't write the next line in the previous line
        if (opts.Verbose)
            Console.Write("\n");

        using (FileStream osb_file = new FileStream(inFile, FileMode.Open))
        {
            using (BinaryReader reader = new BinaryReader(osb_file))
            {
                osb_file.Position = starPos;

                // Make sure this is an OSB
                if (Encoding.UTF8.GetString(reader.ReadBytes(4), 0, 4) == "SOSB")
                {
                    // To Do: Figure these out?
                    byte[] unknown_flag1 = reader.ReadBytes(4);
                    byte[] unknown_flag2 = reader.ReadBytes(4);

                    // Read the number of files in the OSB
                    int numFiles = BitConverter.ToInt32(reader.ReadBytes(4), 0);

                    // Read the offset of the first SOSP chunk
                    // Each SOSP chunk is an entry within the file table.
                    int sospOffset = BitConverter.ToInt32(reader.ReadBytes(4), 0);

                    //Read until SOSP
                    byte[] unknown_data = reader.ReadBytes(sospOffset - (int)reader.BaseStream.Position);

                    List<fileIndex> files = new List<fileIndex>();

                    // Read each SOSP chunk
                    for (int i = 0; i < numFiles; i++)
                    {
                        //Grab header
                        string sosp = Encoding.UTF8.GetString(reader.ReadBytes(4));

                        //Read offset bytes
                        byte[] off1 = reader.ReadBytes(2);
                        byte[] off2 = reader.ReadBytes(2);

                        bool isADPCM = false;
                        int offset = 0;

                        //Calculate Offset

                        // ADPCM flag = off1[1]
                        // This is what that XX in the file offset is used for
                        // 00 = PCM
                        // 01 = ADPCM
                        // 03 = ADPCM but loops(?)

                        if (off1[1] != 0)
                        {
                            isADPCM = true;
                            byte[] adpcm_offset = new byte[] { 0x00, off1[0], off2[1], off2[0] };
                            Array.Reverse(adpcm_offset);
                            offset = BitConverter.ToInt32(adpcm_offset, 0);
                        }
                        else
                        {
                            offset = BitConverter.ToInt32(ReadCustomBytes(off1, off2), 0);
                        }

                        // File length
                        // The number of samples in each sound is laid out as
                        // 02 03 00 01
                        // So, to get the file length:
                        // If PCM: samples * 2
                        // If ADPCM: samples / 2

                        byte[] len1 = reader.ReadBytes(2);
                        byte[] len2 = reader.ReadBytes(2);
                        if(opts.Verbose)
                            Console.WriteLine("DEBUG - " + Path.GetFileNameWithoutExtension(inFile) + "- " + i.ToString() + " Length data: " + BitConverter.ToString(len1) + "-" + BitConverter.ToString(len2));
                        int length = BitConverter.ToInt32(ReadCustomBytes(len1, len2), 0);

                        if (isADPCM)
                        {
                            length = length / 2;
                        }
                        else
                        {
                            length = length * 2;
                        }

                        //Just a simple check for now. Setting length to 0 will skip the file instead of crashing
                        if (length < 0)
                            length = 0;

                        //Store data we don't use for possible analysis when understood (though it appears to be nothing?).
                        List<byte[]> other_data = new List<byte[]>();
                        bool not_endp = true;
                        string endp = "";
                        while (not_endp)
                        {
                            byte[] data = reader.ReadBytes(4);
                            string data_string = Encoding.UTF8.GetString(data, 0, data.Length);
                            switch (data_string) { 
                                case "ENDP":
                                    endp = data_string;
                                    not_endp = false;
                                    break;
                                default:
                                    other_data.Add(data);
                                    break;
                            }
                        }
                        byte[] byte_array = other_data.SelectMany(a => a).ToArray();

                        //Create list of files to read sequentally
                        files.Add(new fileIndex(sosp,  off1.Concat(off2).ToArray(), offset, length, isADPCM, byte_array, endp)) ;
                    }

                    //Brute force file offsets and lengths because the old method overlaps files and "SOSDENDD" markers
                    long old_position = osb_file.Position;
                    foreach (fileIndex fi in files)
                    {
                        if (Encoding.UTF8.GetString(reader.ReadBytes(4), 0, 4) != "SOSD")
                            Console.WriteLine("Something is wrong with file index " + files.IndexOf(fi).ToString());
                        fi.FileOff = (int)osb_file.Position;

                        //Some OSB files have AMKR, NITS, PALP, EMUN, IPTC, RPCE, and ETTX metadata after the audio data.
                        //We need to skip that but continue on to the ENDD magic bytes to accurately brute force file data.
                        bool continue_scanning = true;
                        bool has_metadata = false;
                        long old_filelen = fi.FileLen;
                        while (continue_scanning)
                        {
                            string string_compare = Encoding.UTF8.GetString(reader.ReadBytes(4), 0, 4);
                            switch (string_compare)
                            {
                                case "ENDD":
                                    continue_scanning = false;
                                    if (has_metadata == false)
                                        fi.FileLen = (int)osb_file.Position - fi.FileOff - "ENDD".Length;
                                    break;
                                case "AMKR":
                                    fi.FileLen = (int)osb_file.Position - fi.FileOff - "AMKR".Length;
                                    has_metadata = true;
                                    break;
                                default:
                                    break;
                            }
                        }

                        if (opts.Verbose)
                        {
                            if (old_filelen > fi.FileLen)
                            {
                                Console.WriteLine("FileLen issue with " + Path.GetFileNameWithoutExtension(inFile) + "- " + files.IndexOf(fi).ToString() + " - Original length: " + old_filelen.ToString() + " New length: " + fi.FileLen.ToString());
                            }
                            Console.WriteLine("DEBUG - " + Path.GetFileNameWithoutExtension(inFile) + "- " + files.IndexOf(fi).ToString() + " Offset data: " + BitConverter.ToString(fi.FileOffBytes));
                        }
                        byte[] offCalc = BitConverter.GetBytes(fi.FileOff);
                        Array.Reverse(offCalc);
                        if(opts.Verbose)
                            Console.WriteLine("DEBUG - " + Path.GetFileNameWithoutExtension(inFile) + "- " + files.IndexOf(fi).ToString() + " calculated Offset data: " + BitConverter.ToString(offCalc));
                    }

                    osb_file.Position = old_position;
                    //Advance reader past header and store data if different from the first offset
                    //(Use these once brue forcing isn't necessary)
                    string sosd = Encoding.UTF8.GetString(reader.ReadBytes(4), 0, 4);
                    byte[] sosd_bytes = reader.ReadBytes(files.First().FileOff - (int)reader.BaseStream.Position);

                    // Prep output
                    // Create the output directory if it does not exist
                    if (!Directory.Exists(outDir))
                    {
                        Directory.CreateDirectory(outDir);
                    }
                    string outFile = outDir + Path.DirectorySeparatorChar;
                    string file_prefix = Path.GetFileNameWithoutExtension(inFile);

                    foreach (fileIndex fi in files)
                    {
                        byte[] buffer = reader.ReadBytes(fi.FileLen);
                        string file_suffix = files.IndexOf(fi).ToString().PadLeft(files.Count.ToString().Length);
                        int sample_rate = 44100;
                        if (opts.Sample)
                        {
                            file_suffix += "-(32kHz)";
                            sample_rate = 32000;
                        }
                        string file_name = outFile + file_prefix + "-" + file_suffix;
                        if(opts.Verbose)
                            Console.WriteLine("DEBUG - " + Path.GetFileNameWithoutExtension(inFile) + "- " + files.IndexOf(fi).ToString("00") + " unknown data: " + BitConverter.ToString(fi.Unknown));

                        if (fi.ADPCM) // If this is an ADPCM encoded sound (aka AICA aka Yamaha 4-bit ADPCM), we need to convert it to 16-bit PCM
                        {
                            byte[] newBuffer = ADPCM.adpcm2pcm(in buffer, 0, (uint)buffer.Length);
                            if(opts.Verbose)
                                Console.WriteLine("DEBUG - " + Path.GetFileNameWithoutExtension(inFile) + "- " + files.IndexOf(fi).ToString() + " ADPCM length: " + newBuffer.Length.ToString());
                                PCM.ToWav(newBuffer, newBuffer.Length, file_name + ".wav", sample_rate);

                        }
                        else // Just standard 16-bit PCM
                        {
                            if (buffer.Length != fi.FileLen)
                            {
                                Console.WriteLine("\nError: Buffer for " + Path.GetFileNameWithoutExtension(file_name) + " is greater than defined length! Skipping!");
                            }
                            else
                                PCM.ToWav(buffer, fi.FileLen, file_name + ".wav", sample_rate);

                        }

                        //Calculate next offset
                        int next_offset = 0;
                        if (fi != files.Last())
                            next_offset = files.ElementAt(files.IndexOf(fi) + 1).FileOff;
                        else if (fi == files.Last())
                            next_offset = (int)osb_file.Length;

                        if (opts.Extra) //Only output extra data if specified
                        {
                            if (next_offset > (int)reader.BaseStream.Position)
                            {
                                int extra_data_count = next_offset - (int)reader.BaseStream.Position;
                                if (extra_data_count > 0)
                                {
                                    using (BinaryWriter extra = new BinaryWriter(File.Open(file_name + ".extra_data", FileMode.Create)))
                                    {
                                        extra.Write(reader.ReadBytes(extra_data_count));
                                    }
                                }
                            }
                        }
                        else //Set the reader's position to the next offset
                            reader.BaseStream.Position = next_offset;
                    }
                }
            }
        }
    }
}

