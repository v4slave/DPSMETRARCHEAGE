using System;
using System.IO;
using System.Text;
class TestSelfIdentity {
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static int Main(string[] args){try{
  int self=0,changes=0;string name=null;var decoder=new CombatDecoder(0);int hits=0;
  decoder.Self.Changed=delegate(int id,string n){self=id;name=n;changes++;};
  decoder.OnDamage=delegate(DamageEvent e){if(e.Source==self&&self!=0)hits++;};
  using(var r=new BinaryReader(File.OpenRead(args[0]))){r.BaseStream.Position=20;int link=r.ReadInt32();while(r.BaseStream.Position<r.BaseStream.Length){uint sec=r.ReadUInt32(),us=r.ReadUInt32();int len=r.ReadInt32();r.ReadInt32();byte[] b=r.ReadBytes(len);decoder.Packet(sec+us/1000000.0,b,link);decoder.Packet(sec+us/1000000.0,b,link);}}
  Check(self==112854&&name=="Dor"&&hits==6,"Real login, binding and six outgoing damage events: "+self+" / "+name+" / "+hits);
  Check(changes==2,"Account list / nearby actors must not change self");
  var identity=new SelfIdentity();int selected=0;identity.Changed=delegate(int id,string n){selected=id;};
  byte[] state=new byte[128];state[2]=11;state[3]=3;state[34]=99;state[42]=4;Array.Copy(Encoding.UTF8.GetBytes("Test"),0,state,44,4);
  byte[] buff=new byte[42];buff[2]=20;buff[3]=3;buff[5]=7;buff[8]=99;
  identity.Read(buff);Check(selected==0,"Buff alone cannot identify self");
  identity.Read(state);Check(selected==0,"Private state waits for world binding");
  buff[8]=98;identity.Read(buff);Check(selected==0,"Another player's binding rejected");
  buff[8]=99;identity.Read(buff);Check(selected==7,"Different character and object ID supported");
  state[34]=100;identity.Read(state);Check(selected==0,"Character switch clears old source");
  identity.Read(buff);Check(selected==0,"Previous character's buff rejected");
  state[2]=0x96;state[3]=1;identity.Read(state);Check(selected==0,"Account character list rejected");
  Console.WriteLine("PASS: real Dor login and hits, retransmissions, foreign bindings, character switch, account list rejection");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
