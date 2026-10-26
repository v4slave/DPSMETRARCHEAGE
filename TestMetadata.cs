using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
class TestMetadata {
 static int Main(string[] args){
  var identities=new Dictionary<int,int>();int named=0;
  var decoder=new CombatDecoder(83296);
  decoder.OnIdentity=delegate(int id,int template,string name){identities[id]=template;if(name.Length>0)named++;};
  using(var r=new BinaryReader(File.OpenRead(args[0]))){r.BaseStream.Position=20;int link=r.ReadInt32();while(r.BaseStream.Position<r.BaseStream.Length){uint sec=r.ReadUInt32(),us=r.ReadUInt32();int len=r.ReadInt32();r.ReadInt32();decoder.Packet(sec+us/1000000.0,r.ReadBytes(len),link);}}
  if(identities[95054]!=7512||identities[52055]!=8029||named==0)throw new Exception("Spawn identity regression");
  var names=new TargetNames("captures/test-npc-cache.tsv");names.Observe(95054,7512,"");names.Observe(52055,8029,"");
  if(!names.Get(95054).StartsWith("Легкий манекен")||!names.Get(52055).StartsWith("Мертвый всадник"))throw new Exception("NPC names");
  names.Observe(95054,0,"Другой игрок");if(!names.Get(95054).StartsWith("Другой игрок"))throw new Exception("Object ID reuse");
  var branches=Enumerable.Range(1,14).Count(i=>File.Exists(Path.Combine("assets","branch-"+i+".xml")));
  if(branches!=14||SkillCatalog.Entries.Count<500)throw new Exception("Incomplete skill branches");
  foreach(var row in SkillCatalog.Entries.Values)if(!File.Exists(Path.Combine("assets",row[2])))throw new Exception("Missing icon "+row[2]);
  string skill=SkillCatalog.Key(new DamageEvent{Variant=1,Skill=47018,Ability=44202});
  if(SkillCatalog.Name(skill).StartsWith("Умение"))throw new Exception("Ability ID lookup");
  foreach(int id in new[]{36473,50987,40739})if(!SkillCatalog.Entries.ContainsKey(id))throw new Exception("Screenshot skill missing "+id);
  Console.WriteLine("PASS: observed NPC identities and reused IDs; 14 branches, "+SkillCatalog.Entries.Keys.Count(i=>i>0)+" skill IDs, "+SkillCatalog.Entries.Keys.Count(i=>i<0)+" effects and icon references.");return 0;
 }
}

