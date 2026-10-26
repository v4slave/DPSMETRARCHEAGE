using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
public sealed class GameConnections {
    [DllImport("iphlpapi.dll")]static extern uint GetExtendedTcpTable(IntPtr table,ref int size,bool order,int family,int tableClass,uint reserved);
    readonly object gate=new object();readonly HashSet<string> known=new HashSet<string>();DateTime refreshed=DateTime.MinValue;
    static string Key(uint local,int lp,uint remote,int rp){return local+":"+lp+":"+remote+":"+rp;}
    void Refresh(){
        var ids=new HashSet<int>();foreach(var p in Process.GetProcessesByName("archeage")){ids.Add(p.Id);p.Dispose();}
        int size=0;uint result=GetExtendedTcpTable(IntPtr.Zero,ref size,false,2,5,0);if(result!=122&&result!=0)return;
        IntPtr ptr=Marshal.AllocHGlobal(size);try{
            if(GetExtendedTcpTable(ptr,ref size,false,2,5,0)!=0)return;int count=Marshal.ReadInt32(ptr);byte[] row=new byte[24];
            known.Clear();
            for(int i=0;i<count;i++){Marshal.Copy(IntPtr.Add(ptr,4+i*24),row,0,24);if(!ids.Contains(BitConverter.ToInt32(row,20)))continue;
                int lp=row[8]*256+row[9],rp=row[16]*256+row[17];if(rp==1239)known.Add(Key(BitConverter.ToUInt32(row,4),lp,BitConverter.ToUInt32(row,12),rp));}
            refreshed=DateTime.UtcNow;
        }finally{Marshal.FreeHGlobal(ptr);}
    }
    public bool Contains(byte[] packet,int link){
        int ip=link==12?0:link==1?14:link==0?4:-1;if(ip<0||packet.Length<ip+20||packet[ip]>>4!=4||packet[ip+9]!=6)return false;
        int tcp=ip+(packet[ip]&15)*4;if(packet.Length<tcp+20)return false;
        int sp=packet[tcp]*256+packet[tcp+1],dp=packet[tcp+2]*256+packet[tcp+3];if(sp!=1239&&dp!=1239)return false;
        uint source=BitConverter.ToUInt32(packet,ip+12),destination=BitConverter.ToUInt32(packet,ip+16);
        string key=sp==1239?Key(destination,dp,source,sp):Key(source,sp,destination,dp);
        lock(gate){if((DateTime.UtcNow-refreshed).TotalSeconds>1)Refresh();if(known.Contains(key))return true;if((DateTime.UtcNow-refreshed).TotalMilliseconds<100)return false;Refresh();return known.Contains(key);}
    }
}
