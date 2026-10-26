using System;
using System.IO;
using System.Collections.Generic;
class TestDecoder {
    static int Main(string[] args) {
        int total=0,matches=0; var dots=new List<int>();
        for(int fileIndex=0;fileIndex<args.Length;fileIndex++) {
            string path=args[fileIndex]; bool dotFile=fileIndex==2;
            var decoder=new CombatDecoder(83296);
            decoder.OnDamage=delegate(DamageEvent e){
                if(!dotFile){total++;if(e.Amount==146115)matches++;}
                else if(e.Time>=1792967264 && e.Time<1792967270 && e.Target==52055) dots.Add(e.Amount);
            };
            using(var r=new BinaryReader(File.OpenRead(path))) {
                if(r.ReadUInt32()!=0xa1b2c3d4)throw new Exception("PCAP header");
                r.BaseStream.Position=20;int link=r.ReadInt32();
                while(r.BaseStream.Position<r.BaseStream.Length) {
                    uint sec=r.ReadUInt32(),us=r.ReadUInt32();int len=r.ReadInt32();r.ReadInt32();
                    byte[] data=r.ReadBytes(len);
                    decoder.Packet(sec+us/1000000.0,data,link);
                    decoder.Packet(sec+us/1000000.0,data,link); // Retransmit must not count twice.
                }
            }
            if(decoder.Errors!=0)throw new Exception("Decode errors: "+decoder.Errors);
        }
        if(total!=30 || matches!=1)throw new Exception("Unexpected candidates: "+total+", control hits: "+matches);
        if(args.Length==3 && string.Join(",",dots.ToArray())!="221,224,143,203,136,207") throw new Exception("Unexpected DoT: "+string.Join(",",dots.ToArray()));
        Console.WriteLine("PASS: 30 candidates; control damage 146115 exactly once; duplicate TCP records ignored.");
        if(args.Length==3)Console.WriteLine("PASS: six poison ticks match combat chat, total 1134; duplicate TCP records ignored.");
        return 0;
    }
}
