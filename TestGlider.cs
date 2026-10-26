using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
class TestGlider {
 static int Main(string[] args){try{
  var events=new List<DamageEvent>();var decoder=new CombatDecoder(0);decoder.OnDamage=events.Add;
  using(var r=new BinaryReader(File.OpenRead(args[0]))){r.BaseStream.Position=20;int link=r.ReadInt32();while(r.BaseStream.Position<r.BaseStream.Length){uint sec=r.ReadUInt32(),us=r.ReadUInt32();int len=r.ReadInt32();r.ReadInt32();var b=r.ReadBytes(len);decoder.Packet(sec+us/1000000.0,b,link);decoder.Packet(sec+us/1000000.0,b,link);}}
  var fire=events.Where(e=>e.Target==81437&&e.Variant==2&&e.Skill==33999).ToList();
  var burn=events.Where(e=>e.Target==81437&&e.Variant==2&&e.Skill==33997).ToList();
  if(fire.Count!=5||fire.Sum(e=>e.Amount)!=25000||burn.Count!=5||burn.Sum(e=>e.Amount)!=5000)throw new Exception("Fire/burn totals");
  if(fire.Concat(burn).Any(e=>e.Source!=80036||e.OriginalSource!=81437))throw new Exception("Wrong glider ownership");
  var victim=events.Where(e=>e.Source==81437&&e.Target==80036&&e.Time>=1793003170&&e.Time<1793003177).ToList();
  if(victim.Sum(e=>e.Amount)!=4052||victim.Count!=4)throw new Exception("NPC damage/card regression: "+victim.Sum(e=>e.Amount));
  Console.WriteLine("PASS: 25000 initial fire + 5000 burn assigned to player 80036 through glider 302264; NPC outgoing damage remains 4052 in 4 hits; duplicates ignored.");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
