// Decompiled with JetBrains decompiler
// Type: tsclibnet.get_setting
// Assembly: tsclibnet, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: A64385FF-5635-48AA-8C98-BF7EE2302ADD
// Assembly location: C:\workspaces\drivers\tsc-printer\TSC C# SDK 20210323\x64\tsclibnet.dll

using System;


namespace tsclibnet
{
  internal class get_setting
  {
    public string[] get_array(string result)
    {
      string str = "";
      bool flag = false;
      int newSize = 0;
      string[] array = new string[256];
      for (int startIndex = 0; startIndex <= result.Length - 1; ++startIndex)
      {
        if (result.Substring(startIndex, 1) == "|")
          flag = true;
        else if (result.Substring(startIndex, 1) == "}")
        {
          array[newSize] = str;
          str = "";
          flag = false;
          ++newSize;
          Array.Resize<string>(ref array, newSize);
        }
        else if (flag)
          str += result.Substring(startIndex, 1);
      }
      return array;
    }

    public string get_diagcommand_value(string value)
    {
      bool flag = false;
      string diagcommandValue = "";
      for (int startIndex = 0; startIndex <= value.Length - 1; ++startIndex)
      {
        if (value.Substring(startIndex, 1) == "|")
        {
          flag = true;
        }
        else
        {
          if (value.Substring(startIndex, 1) == "}")
          {
            diagcommandValue.Replace("\r\n", "");
            break;
          }
          if (flag)
            diagcommandValue += value.Substring(startIndex, 1);
        }
      }
      return diagcommandValue;
    }

    public byte[] bit_array2byte_array(byte[] data)
    {
      int length = (data.Length + 7) / 8;
      byte[] numArray = new byte[length];
      for (int index = 0; index < length; ++index)
        numArray[index] = (byte) 0;
      for (int index = 0; index <= data.Length - 1; ++index)
      {
        if (data[index] == (byte) 1)
          numArray[index / 8] ^= (byte) (128 >> index % 8);
      }
      return numArray;
    }
  }
}
