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
        int _fileOff;
        int _fileLen;
        bool _adpcm;
        byte[] _unknown;
        string _strFooter;

        public fileIndex(string strHeader, int fileOff, int fileLen, bool adpcm, byte[] unknown, string strFooter)
        {
            this._strHeader = strHeader;
            this._fileOff = fileOff;
            this._fileLen = fileLen;
            this._adpcm = adpcm;
            this._unknown = unknown;
            this._strFooter = strFooter;
        }

        public string StrHeader { get { return _strHeader; } }
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

    public static void Extract(long starPos, string inFile, string outDir)
    {
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

                        // ADPCM flag
                        // This is what that XX in the file offset is used for
                        // 00 = PCM
                        // 01 = ADPCM
                        // 03 = ???
                        if (off1[1] == 1)
                        {
                            isADPCM = true;
                            byte[] adpcm_offset = new byte[] { 0x00, off1[0], off2[1], off2[0] };
                            Array.Reverse(adpcm_offset);
                            offset = BitConverter.ToInt32(adpcm_offset, 0);
                        }
                        else if (off1[1] == 3)
                        {
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
                        int length = BitConverter.ToInt32(ReadCustomBytes(reader.ReadBytes(2), reader.ReadBytes(2)), 0);

                        if (isADPCM == true)
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
                            if (data_string != "ENDP")
                            {
                                other_data.Add(data);
                            }
                            else
                            {
                                endp = data_string;
                                not_endp = false;
                            }
                        }
                        byte[] byte_array = other_data.SelectMany(a => a).ToArray();

                        //Create list of files to read sequentally
                        files.Add(new fileIndex(sosp, offset, length, isADPCM, byte_array, endp));
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
                        while (continue_scanning)
                        {
                            string string_compare = Encoding.UTF8.GetString(reader.ReadBytes(4), 0, 4);
                            if (string_compare == "ENDD")
                            {
                                continue_scanning = false;
                                if (has_metadata == false)
                                    fi.FileLen = (int)osb_file.Position - fi.FileOff - "ENDD".Length;

                            }
                            else if (string_compare == "AMKR")
                            {
                                fi.FileLen = (int)osb_file.Position - fi.FileOff - "AMKR".Length;
                                has_metadata = true;
                            }
                        }

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
                        //Console.WriteLine(fi.FileOff.ToString() + " versus " + osb_file.Position.ToString());
                        byte[] buffer = reader.ReadBytes(fi.FileLen);
                        string file_name = outFile + file_prefix + "-" + files.IndexOf(fi).ToString().PadLeft(files.Count.ToString().Length);

                        if (fi.ADPCM) // If this is an ADPCM encoded sound (aka AICA aka Yamaha 4-bit ADPCM), we need to convert it to 16-bit PCM
                        {
                            byte[] newBuffer = ADPCM.ToRaw(buffer, 0, buffer.Length);
                            PCM.ToWav(newBuffer, newBuffer.Length, file_name + ".wav");
                        }
                        else // Just standard 16-bit PCM
                        {
                            if (buffer.Length != fi.FileLen)
                            {
                                Console.WriteLine("\nError: Buffer for " + Path.GetFileNameWithoutExtension(file_name) + " is greater than defined length! Skipping!");
                            }
                            else
                            {
                                PCM.ToWav(buffer, fi.FileLen, file_name + ".wav");
                            }
                        }

                        if (fi != files.Last())
                        {
                            int next_offset = files.ElementAt(files.IndexOf(fi) + 1).FileOff;
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

                    }
                }
            }
        }
    }
}

