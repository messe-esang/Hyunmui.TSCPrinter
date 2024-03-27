// Decompiled with JetBrains decompiler
// Type: TSCSDK.file
// Assembly: tsclibnet, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: A64385FF-5635-48AA-8C98-BF7EE2302ADD
// Assembly location: C:\workspaces\drivers\tsc-printer\TSC C# SDK 20210323\x64\tsclibnet.dll

using System;
using System.IO;
using System.Text;



namespace TSCSDK
{
    public class file
    {
        public void savetofile(byte[] data)
        {
            using (StreamWriter streamWriter = new StreamWriter("savefile.txt"))
            {
                streamWriter.Write((object)data);
                streamWriter.Dispose();
                streamWriter.Close();
            }
        }

        public void savetofile_string(string data)
        {
            using (StreamWriter streamWriter = new StreamWriter("savefile.txt"))
            {
                streamWriter.Write(data);
                streamWriter.Dispose();
                streamWriter.Close();
            }
        }

        public int sendASCtoHEX(string hexString)
        {
            byte[] buffer = new byte[hexString.Length / 2];
            for (int startIndex = 0; startIndex < hexString.Length; startIndex += 2)
                buffer[startIndex / 2] = Convert.ToByte(hexString.Substring(startIndex, 2), 16);
            FileStream fileStream = new FileStream("C:\\\\savefile.txt", FileMode.Create);
            fileStream.Seek(0L, SeekOrigin.Begin);
            fileStream.Write(buffer, 0, buffer.Length);
            fileStream.Close();
            return 1;
        }

        public string about()
        {
            return "test";
        }
    }
}
