using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Collections.Generic;
public static class Startup {
 [DllImport("iphlpapi.dll",SetLastError=true)]static extern uint GetExtendedTcpTable(IntPtr table,ref int size,bool order,int family,int tableClass,uint reserved);
 public static string[] Prepare(){
  string npcap=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"Npcap","wpcap.dll");
  if(!File.Exists(npcap))throw new InvalidOperationException("Не найден Npcap. Установи Npcap для захвата игрового трафика.");
  string folder=Path.Combine(AppPaths.Data,"captures","meter-"+DateTime.Now.ToString("yyyyMMdd-HHmmss-fff"));Directory.CreateDirectory(folder);
  string filter=Path.Combine(folder,"filter.txt");File.WriteAllText(filter,"tcp port 1239",System.Text.Encoding.ASCII);
  File.WriteAllText(Path.Combine(AppPaths.Data,"active-capture.txt"),folder);
  return new[]{folder,filter,"900","0"};
 }
}
