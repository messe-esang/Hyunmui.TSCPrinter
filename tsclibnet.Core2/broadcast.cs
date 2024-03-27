// Decompiled with JetBrains decompiler
// Type: TSCSDK.broadcast
// Assembly: tsclibnet, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: A64385FF-5635-48AA-8C98-BF7EE2302ADD
// Assembly location: C:\workspaces\drivers\tsc-printer\TSC C# SDK 20210323\x64\tsclibnet.dll

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TSCSDK
{
  public class broadcast
  {
    private static Socket tempSocket;
    private static EndPoint ipe;
    public string[] udp_printername = new string[128];
    public string[] udp_macaddress = new string[128];
    public string[] udp_ipaddress = new string[128];
    public string[] udp_version = new string[128];
    public string[] udp_modelname = new string[128];
    public byte[] udp_status = new byte[128];
    private byte[] TSCUDP = new byte[32];
    private byte[] inbuffer = new byte[65536];
    public string BroadcastPrinterName;
    public string BroadcastIPAddress;
    public string BroadcastMACAddress;
    public string BroadcastModelName;
    public string BroadcastPrnterVersion;
    public int BroadcastPrinterStatus;

    private void TSCUDP_Data()
    {
      this.TSCUDP[0] = (byte) 0;
      this.TSCUDP[1] = (byte) 50;
      this.TSCUDP[2] = (byte) 0;
      this.TSCUDP[3] = (byte) 1;
      this.TSCUDP[4] = (byte) 0;
      this.TSCUDP[5] = (byte) 1;
      this.TSCUDP[6] = (byte) 1;
      this.TSCUDP[7] = (byte) 0;
      this.TSCUDP[8] = (byte) 0;
      this.TSCUDP[9] = (byte) 2;
      this.TSCUDP[10] = (byte) 0;
      this.TSCUDP[11] = (byte) 0;
      this.TSCUDP[12] = (byte) 0;
      this.TSCUDP[13] = (byte) 1;
      this.TSCUDP[14] = (byte) 0;
      this.TSCUDP[15] = (byte) 0;
      this.TSCUDP[16] = (byte) 1;
      this.TSCUDP[17] = (byte) 0;
      this.TSCUDP[18] = (byte) 0;
      this.TSCUDP[19] = (byte) 0;
      this.TSCUDP[20] = (byte) 0;
      this.TSCUDP[21] = (byte) 0;
      this.TSCUDP[22] = byte.MaxValue;
      this.TSCUDP[23] = byte.MaxValue;
      this.TSCUDP[24] = byte.MaxValue;
      this.TSCUDP[25] = byte.MaxValue;
      this.TSCUDP[26] = byte.MaxValue;
      this.TSCUDP[27] = byte.MaxValue;
      this.TSCUDP[28] = (byte) 0;
      this.TSCUDP[29] = (byte) 0;
      this.TSCUDP[30] = (byte) 0;
      this.TSCUDP[31] = (byte) 0;
    }

    public string TscUDP_Run(int delay)
    {
      this.TSCUDP_Data();
      Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
      socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 2000);
      socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 2000);
      socket.EnableBroadcast = true;
      socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
      EndPoint remoteEP1 = (EndPoint) new IPEndPoint(IPAddress.Broadcast, 22368);
      IPEndPoint ipEndPoint1 = new IPEndPoint(IPAddress.Any, 0);
      socket.SendTo(this.TSCUDP, remoteEP1);
      IPEndPoint ipEndPoint2 = new IPEndPoint(IPAddress.Any, 0);
      EndPoint remoteEP2 = remoteEP1;
      int index = 0;
      while (true)
      {
        try
        {
          Thread.Sleep(delay);
          if (socket.ReceiveFrom(this.inbuffer, ref remoteEP2) > 0)
          {
            this.udp_modelname[index] = this.printer_modelname();
            this.udp_ipaddress[index] = this.printer_ipaddress();
            this.udp_macaddress[index] = this.printer_macaddress();
            this.udp_printername[index] = this.printer_ethernetname();
            this.udp_version[index] = this.printer_version();
            this.udp_status[index] = this.printerstatus();
            ++index;
          }
        }
        catch
        {
          break;
        }
      }
      string str = Encoding.UTF8.GetString(this.inbuffer);
      socket.Close();
      return str;
    }

    private string printer_macaddress()
    {
      string str1 = "";
      for (int index = 0; index <= 5; ++index)
      {
        if (index == 1 || index == 2 || index == 3 || index == 4 || index == 5)
          str1 += ":";
        string str2 = Convert.ToString(this.inbuffer[22 + index], 16).ToUpper();
        if (str2.Length < 2)
          str2 = "0" + str2;
        str1 += str2;
      }
      return str1;
    }

    private string printer_ipaddress()
    {
      string str1 = "";
      for (int index = 0; index <= 3; ++index)
      {
        if (index == 1 || index == 2 || index == 3)
          str1 += ".";
        string str2 = Convert.ToString(this.inbuffer[44 + index]);
        str1 += str2;
      }
      return str1;
    }

    private string printer_modelname()
    {
      string str = "";
      for (int count = 0; count <= 15; ++count)
      {
        if (this.inbuffer[52 + count] == (byte) 0)
        {
          str = Encoding.ASCII.GetString(this.inbuffer, 52, count);
          break;
        }
      }
      return str;
    }

    private string printer_ethernetname()
    {
      string str = "";
      for (int count = 0; count <= 15; ++count)
      {
        if (this.inbuffer[84 + count] == (byte) 0)
        {
          str = Encoding.ASCII.GetString(this.inbuffer, 84, count);
          break;
        }
      }
      return str;
    }

    private string printer_version()
    {
      string str = "";
      for (int count = 0; count <= 12; ++count)
      {
        if (this.inbuffer[68 + count] == (byte) 0)
        {
          str = Encoding.ASCII.GetString(this.inbuffer, 68, count);
          break;
        }
      }
      return str;
    }

    private byte printerstatus()
    {
      if (this.inbuffer[40] == (byte) 0 && this.inbuffer[41] == (byte) 0)
        return 0;
      if (this.inbuffer[40] == (byte) 32 && this.inbuffer[41] == (byte) 0)
        return 16;
      if (this.inbuffer[40] == (byte) 1 && this.inbuffer[41] == (byte) 0)
        return 32;
      if (this.inbuffer[40] == (byte) 3 && this.inbuffer[41] == (byte) 1)
        return 1;
      if (this.inbuffer[40] == (byte) 3 && this.inbuffer[41] == (byte) 2)
        return 2;
      if (this.inbuffer[40] == (byte) 3 && this.inbuffer[41] == (byte) 3)
        return 3;
      if (this.inbuffer[40] == (byte) 3 && this.inbuffer[41] == (byte) 4)
        return 4;
      if (this.inbuffer[40] == (byte) 3 && this.inbuffer[41] == (byte) 5)
        return 5;
      if (this.inbuffer[40] == (byte) 3 && this.inbuffer[41] == (byte) 8)
        return 8;
      if (this.inbuffer[40] == (byte) 3 && this.inbuffer[41] == (byte) 9)
        return 9;
      if (this.inbuffer[40] == (byte) 3 && this.inbuffer[41] == (byte) 16)
        return 10;
      if (this.inbuffer[40] == (byte) 3 && this.inbuffer[41] == (byte) 17)
        return 11;
      if (this.inbuffer[40] == (byte) 3 && this.inbuffer[41] == (byte) 18)
        return 12;
      return this.inbuffer[40] == (byte) 3 && this.inbuffer[41] == (byte) 64 ? (byte) 128 : byte.MaxValue;
    }
  }
}
