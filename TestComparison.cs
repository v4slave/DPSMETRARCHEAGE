using System;
using System.Collections.Generic;
using System.Linq;
class TestComparison {
 static int Main(){
  var events=new List<DamageEvent>{
   new DamageEvent{Source=1,Target=10,Amount=100},new DamageEvent{Source=2,Target=10,Amount=300},
   new DamageEvent{Source=1,Target=20,Amount=400},new DamageEvent{Source=3,Target=20,Amount=0}};
  var all=ActorDamage.Build(events,0);var target=ActorDamage.Build(events,10);
  if(all.Count!=3||all[0].Id!=1||all[0].Damage!=500||all.Sum(r=>r.Damage)!=800)throw new Exception("Ranking or totals");
  if(target.Count!=2||target[0].Id!=2||target[0].Damage!=300||target[0].Share!=75||target[1].Share!=25)throw new Exception("Target filter or shares");
  if(ActorDamage.Build(events,99).Count!=0||ActorDamage.Build(new List<DamageEvent>(),0).Count!=0)throw new Exception("Empty result");
  var zeros=ActorDamage.Build(new List<DamageEvent>{new DamageEvent{Source=1,Amount=0}},0);
  if(zeros[0].Share!=0)throw new Exception("Zero total");
  var large=ActorDamage.Build(new List<DamageEvent>{new DamageEvent{Source=1,Amount=int.MaxValue},new DamageEvent{Source=1,Amount=int.MaxValue}},0);
  if(large[0].Damage!=4294967294L)throw new Exception("Damage overflow");
  Console.WriteLine("PASS: comparison ranking, totals, target filter, shares, empty/zero and large totals.");return 0;
 }
}
