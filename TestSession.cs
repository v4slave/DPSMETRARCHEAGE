using System;
using System.IO;
using System.Collections.Generic;
class TestSession {
 static int Main(string[] args){try{
  var own=new List<int>();int other=0;var decoder=new CombatDecoder(0);
  decoder.OnDamage=delegate(DamageEvent e){if(e.Source==80036)own.Add(e.Amount);else other++;};
  using(var r=new BinaryReader(File.OpenRead(args[0]))){r.BaseStream.Position=20;int link=r.ReadInt32();while(r.BaseStream.Position<r.BaseStream.Length){uint sec=r.ReadUInt32(),us=r.ReadUInt32();int len=r.ReadInt32();r.ReadInt32();var b=r.ReadBytes(len);decoder.Packet(sec+us/1000000.0,b,link);decoder.Packet(sec+us/1000000.0,b,link);}}
  foreach(int amount in new[]{18539,6888,26849,27113,22238,5207,99779})if(own.FindAll(x=>x==amount).Count!=1)throw new Exception("Missing or duplicated chat hit "+amount);
  if(other==0||decoder.Errors!=0)throw new Exception("Source separation / decode error");
  Console.WriteLine("PASS: current character 80036, all seven screenshot hits exactly once, other sources kept separately.");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
