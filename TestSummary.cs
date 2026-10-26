using System;
using System.Collections.Generic;
using System.Linq;
class TestSummary {
 static int Main(){
  var events=new List<DamageEvent>{new DamageEvent{Time=10,Target=1,Amount=100,Skill=55540,Variant=1},new DamageEvent{Time=12,Target=2,Amount=50,Skill=55546,Variant=1},new DamageEvent{Time=14,Target=1,Amount=200,Skill=51998,Variant=1}};
  var all=DamageSummary.Build(events,0);var one=DamageSummary.Build(events,1);var two=DamageSummary.Build(events,2);
  if(all.Total!=350||one.Damage!=300||two.Damage!=50||all.Duration!=4||one.Dps(one.Damage)!=75||two.Dps(two.Damage)!=12.5)throw new Exception("Target aggregation");
  if(one.Dps(one.Damage)+two.Dps(two.Damage)!=all.Dps(all.Total))throw new Exception("DPS is not additive");
  if(events.GroupBy(SkillCatalog.Key).Count()!=2)throw new Exception("Skill variants not grouped");
  if(!double.IsNaN(DamageSummary.Build(new List<DamageEvent>{events[0]},0).Dps(100)))throw new Exception("Single event DPS");
  if(DamageSummary.Build(new List<DamageEvent>(),0).Damage!=0)throw new Exception("Empty");
  if(SkillCatalog.Key(new DamageEvent{Skill=21999,Variant=1})==SkillCatalog.Key(new DamageEvent{Skill=21999,Variant=2}))throw new Exception("Variant collision");
  var vengeance=new[]{new DamageEvent{Variant=1,Ability=39666,Amount=106884},new DamageEvent{Variant=1,Ability=39667,Amount=87160},new DamageEvent{Variant=1,Ability=39668,Amount=34532}};
  if(vengeance.GroupBy(SkillCatalog.Key).Count()!=1||vengeance.Sum(e=>e.Amount)!=228576)throw new Exception("Vengeance aliases");
  var selfNames=new TargetNames("captures/test-npc-cache.tsv");selfNames.Observe(80036,0,"Blackholeinmyass");if(!selfNames.Get(80036).StartsWith("Blackholeinmyass"))throw new Exception("Self target name");
  if(!SkillCatalog.Entries.ContainsKey(39282)||!System.IO.File.Exists(System.IO.Path.Combine("assets",SkillCatalog.Entries[39282][2])))throw new Exception("NPC icon metadata");
  if(SkillCatalog.Name(SkillCatalog.Key(new DamageEvent{Variant=2,Skill=23162}))!="Электрошок")throw new Exception("Screenshot NPC DoT");
  if(SkillCatalog.Name(SkillCatalog.Key(new DamageEvent{Variant=1,Ability=39173}))!="Мощный удар")throw new Exception("Screenshot NPC skill");
  if(SkillCatalog.Key(new DamageEvent{Variant=1,Ability=23162})==SkillCatalog.Key(new DamageEvent{Variant=2,Skill=23162}))throw new Exception("Skill/effect ID namespaces");
  var screenshot=new List<DamageEvent>{new DamageEvent{Time=0,Target=81437,Amount=30000},new DamageEvent{Time=1,Target=80036,Amount=2489},new DamageEvent{Time=2,Target=80036,Amount=1198},new DamageEvent{Time=3,Target=80036,Amount=228},new DamageEvent{Time=6.11,Target=80036,Amount=137}};
  var card=DamageSummary.Build(screenshot,80036);if(card.Total!=34052||card.Damage!=4052||card.Selected.Sum(e=>(long)e.Amount)!=card.Damage||Math.Round(card.Dps(card.Damage))!=663)throw new Exception("Filtered card amount");
  Console.WriteLine("PASS: target filters, additive DPS, skill grouping, empty/single event, variant namespaces.");return 0;
 }
}

