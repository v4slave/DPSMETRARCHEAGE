using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using System.Collections.Generic;

// Passive capture only. No packet transmission or game-process access.
class Capture {
    public static Action<double, byte[], int> OnPacket;
    public static Func<byte[],int,bool> AcceptPacket;
    const string Dll = "wpcap.dll";
    [DllImport("kernel32.dll", CharSet=CharSet.Unicode)] static extern bool SetDllDirectory(string path);
    [DllImport(Dll, CallingConvention=CallingConvention.Cdecl)] static extern int pcap_findalldevs(out IntPtr devs, StringBuilder err);
    [DllImport(Dll, CallingConvention=CallingConvention.Cdecl)] static extern void pcap_freealldevs(IntPtr devs);
    [DllImport(Dll, CallingConvention=CallingConvention.Cdecl)] static extern IntPtr pcap_open_live(string dev, int snap, int promisc, int timeout, StringBuilder err);
    [DllImport(Dll, CallingConvention=CallingConvention.Cdecl)] static extern int pcap_compile(IntPtr p, ref Bpf b, string filter, int optimize, uint mask);
    [DllImport(Dll, CallingConvention=CallingConvention.Cdecl)] static extern int pcap_setfilter(IntPtr p, ref Bpf b);
    [DllImport(Dll, CallingConvention=CallingConvention.Cdecl)] static extern void pcap_freecode(ref Bpf b);
    [DllImport(Dll, CallingConvention=CallingConvention.Cdecl)] static extern int pcap_setnonblock(IntPtr p, int nonblock, StringBuilder err);
    [DllImport(Dll, CallingConvention=CallingConvention.Cdecl)] static extern int pcap_setmintocopy(IntPtr p, int size);
    [DllImport(Dll, CallingConvention=CallingConvention.Cdecl)] static extern int pcap_next_ex(IntPtr p, out IntPtr header, out IntPtr data);
    [DllImport(Dll, CallingConvention=CallingConvention.Cdecl)] static extern int pcap_datalink(IntPtr p);
    [DllImport(Dll, CallingConvention=CallingConvention.Cdecl)] static extern IntPtr pcap_geterr(IntPtr p);
    [DllImport(Dll, CallingConvention=CallingConvention.Cdecl)] static extern int pcap_stats(IntPtr p, IntPtr stats);
    [DllImport(Dll, CallingConvention=CallingConvention.Cdecl)] static extern void pcap_close(IntPtr p);
    [StructLayout(LayoutKind.Sequential)] struct Dev { public IntPtr next, name, description, addresses; public uint flags; }
    [StructLayout(LayoutKind.Sequential)] struct Bpf { public uint count; public IntPtr instructions; }
    static string root, filter; static DateTime end; static int opened; static long totalBytes;
    static readonly object gate = new object();
    static void Log(string s) { lock(gate) { Console.WriteLine(DateTime.UtcNow.ToString("o") + " " + s); Console.Out.Flush(); } }
    static void Record(string name, int index) {
        IntPtr p = IntPtr.Zero; long packets = 0, bytes = 0;
        try {
            var err = new StringBuilder(512);
            p = pcap_open_live(name, 65535, 0, 100, err);
            if (p == IntPtr.Zero) throw new Exception(err.ToString());
            var b = new Bpf();
            if (pcap_compile(p, ref b, filter, 1, 0xffffffff) != 0) throw new Exception(Marshal.PtrToStringAnsi(pcap_geterr(p)));
            try { if (pcap_setfilter(p, ref b) != 0) throw new Exception(Marshal.PtrToStringAnsi(pcap_geterr(p))); }
            finally { pcap_freecode(ref b); }
            if (pcap_setnonblock(p, 1, err) != 0) throw new Exception(err.ToString());
            pcap_setmintocopy(p, 1);
            int link = pcap_datalink(p);
            using (var w = new BinaryWriter(new FileStream(Path.Combine(root, "adapter-" + index + ".pcap"),FileMode.Create,FileAccess.Write,FileShare.ReadWrite))) {
                w.Write(0xa1b2c3d4u); w.Write((ushort)2); w.Write((ushort)4); w.Write(0); w.Write(0u); w.Write(65535u); w.Write((uint)link);
                w.Flush();
                Interlocked.Increment(ref opened);
                Log("READY adapter=" + index + " link=" + link + " device=" + name);
                DateTime flush = DateTime.UtcNow;
                while (DateTime.UtcNow < end && !File.Exists(Path.Combine(root, "STOP")) && Interlocked.Read(ref totalBytes) < 128L*1024*1024) {
                    IntPtr h, data; int result = pcap_next_ex(p, out h, out data);
                    if (result == 0) { Thread.Sleep(10); continue; }
                    if (result < 0) throw new Exception("Capture stopped: " + Marshal.PtrToStringAnsi(pcap_geterr(p)));
                    int len = Marshal.ReadInt32(h, 8);
                    if (len < 0 || len > 65535) throw new Exception("Invalid captured length");
                    byte[] packet = new byte[len]; Marshal.Copy(data, packet, 0, len);
                    if(AcceptPacket!=null&&!AcceptPacket(packet,link))continue;
                    if (OnPacket != null) OnPacket(Marshal.ReadInt32(h, 0) + Marshal.ReadInt32(h, 4)/1000000.0, packet, link);
                    w.Write(Marshal.ReadInt32(h, 0)); w.Write(Marshal.ReadInt32(h, 4)); w.Write(len); w.Write(Marshal.ReadInt32(h, 12)); w.Write(packet);
                    packets++; bytes += len; Interlocked.Add(ref totalBytes, len + 16);
                    if ((DateTime.UtcNow - flush).TotalSeconds >= 1) { w.Flush(); flush = DateTime.UtcNow; }
                }
            }
            IntPtr stats = Marshal.AllocHGlobal(256);
            try { if (pcap_stats(p, stats) == 0) Log("STATS adapter="+index+" received="+Marshal.ReadInt32(stats)+" dropped="+Marshal.ReadInt32(stats,4)); }
            finally { Marshal.FreeHGlobal(stats); }
        } catch(Exception e) { Log("ERROR adapter="+index+" "+e.Message); }
        finally { if (p != IntPtr.Zero) pcap_close(p); Log("DONE adapter="+index+" packets="+packets+" bytes="+bytes); }
    }
    static int Main(string[] args) { return Run(args); }
    public static int Run(string[] args) {
        if (args.Length != 3) { Console.Error.WriteLine("Capture.exe OUTPUT_DIR FILTER_FILE SECONDS"); return 2; }
        root = Path.GetFullPath(args[0]); Directory.CreateDirectory(root); filter = File.ReadAllText(args[1]).Trim(); opened = 0; totalBytes = 0;
        if (filter.Length == 0) return 2;
        end = DateTime.UtcNow.AddSeconds(int.Parse(args[2]));
        SetDllDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "Npcap"));
        IntPtr head; var err = new StringBuilder(512);
        if (pcap_findalldevs(out head, err) != 0) { Log(err.ToString()); return 1; }
        var threads = new List<Thread>();
        try { int i = 0; for(IntPtr ptr = head; ptr != IntPtr.Zero;) {
            Dev d = (Dev)Marshal.PtrToStructure(ptr, typeof(Dev));
            string name = Marshal.PtrToStringAnsi(d.name); int index = i++;
            Thread t = new Thread(delegate() { Record(name, index); }); threads.Add(t); t.Start(); ptr = d.next;
        }} finally { pcap_freealldevs(head); }
        foreach (var t in threads) t.Join();
        return opened > 0 ? 0 : 1;
    }
}
