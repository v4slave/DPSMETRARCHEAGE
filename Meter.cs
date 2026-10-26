using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using System.Globalization;
using System.Text;

// Experimental decoder for the observed RU client build, NOT all combat formats.
public sealed class DamageEvent {
    public double Time; public int Source, Target, Skill, Amount, Variant, Ability, OriginalSource;
}
public sealed class CombatDecoder {
    public readonly SelfIdentity Self=new SelfIdentity();
    public Action<int,int,string> OnIdentity;
    sealed class Flow {
        public uint Next; public bool Started, Failed;
        public List<byte> Buffer = new List<byte>();
        public Dictionary<uint,byte[]> Pending = new Dictionary<uint,byte[]>();
    }
    readonly Dictionary<string,Flow> flows = new Dictionary<string,Flow>();
    sealed class EffectOrigin {public int ObjectId;public double Time;}
    readonly Dictionary<string,EffectOrigin> effectOrigins=new Dictionary<string,EffectOrigin>();
    readonly Dictionary<int,EffectOrigin> gliderOrigins=new Dictionary<int,EffectOrigin>();
    readonly Dictionary<int,int> objectOwners=new Dictionary<int,int>();
    readonly List<KeyValuePair<int,DamageEvent>> pendingOwners=new List<KeyValuePair<int,DamageEvent>>();
    public int SourceId;
    public int Unsupported, Errors, Frames, Events;
    public Action<DamageEvent> OnDamage;
    public CombatDecoder(int source) { SourceId = source; }
    public void Complete(){lock(flows){foreach(var entry in pendingOwners){entry.Value.Source=entry.Key;Emit(entry.Value);}pendingOwners.Clear();}}
    static int U16(byte[] b,int p) { return b[p] | b[p+1]<<8; }
    static int U24(byte[] b,int p) { return b[p] | b[p+1]<<8 | b[p+2]<<16; }
    static uint BE32(byte[] b,int p) { return ((uint)b[p]<<24)|((uint)b[p+1]<<16)|((uint)b[p+2]<<8)|b[p+3]; }
    static string EffectKey(int target,int effect,uint index){return target+":"+effect+":"+index;}
    void Emit(DamageEvent e){if(SourceId!=0&&e.Source!=SourceId)return;Events++;if(OnDamage!=null)OnDamage(e);}
    void ResolveOwner(int objectId,int owner){objectOwners[objectId]=owner;for(int i=pendingOwners.Count-1;i>=0;i--)if(pendingOwners[i].Key==objectId){var e=pendingOwners[i].Value;pendingOwners.RemoveAt(i);e.Source=owner;Emit(e);}}
    public void Packet(double time, byte[] b,int link) {
        lock(flows) {
            int ip = link==12 ? 0 : link==1 ? 14 : link==0 ? 4 : -1;
            if(ip<0 || b.Length<ip+20 || b[ip]>>4!=4 || b[ip+9]!=6) return;
            if (((b[ip+6]<<8|b[ip+7]) & 0x3fff)!=0) { Errors++; return; }
            int tcp=ip+(b[ip]&15)*4;
            if(b.Length<tcp+20) return;
            int sport=b[tcp]*256+b[tcp+1], dport=b[tcp+2]*256+b[tcp+3];
            // This prototype supports the game stream observed on TCP 1239.
            if(sport!=1239) return;
            int start=tcp+(b[tcp+12]>>4)*4, end=Math.Min(b.Length,ip+(b[ip+2]<<8|b[ip+3]));
            string key=BitConverter.ToString(b,ip+12,8)+":"+sport+":"+dport;
            if((b[tcp+13]&2)!=0)flows.Remove(key);
            if(start>=end) return;
            Flow f;
            if(!flows.TryGetValue(key,out f)) { f=new Flow(); flows[key]=f; }
            if(f.Failed) return;
            uint seq=BE32(b,tcp+4); byte[] data=new byte[end-start]; Array.Copy(b,start,data,0,data.Length);
            if(!f.Started) { f.Started=true; f.Next=seq; }
            int delta=unchecked((int)(seq-f.Next));
            if(delta>0) {
                f.Pending[seq]=data;
                if(f.Pending.Count>256) { f.Failed=true; Errors++; }
                return;
            }
            Append(f, seq, data);
            bool progress=true;
            while(progress) {
                progress=false;
                foreach(var pair in new List<KeyValuePair<uint,byte[]>>(f.Pending)) {
                    if(unchecked((int)(pair.Key-f.Next))<=0) { f.Pending.Remove(pair.Key); Append(f,pair.Key,pair.Value); progress=true; }
                }
            }
            while(f.Buffer.Count>=4) {
                int size=(f.Buffer[0]|f.Buffer[1]<<8)+2;
                if(size<4 || f.Buffer[2]!=0xdd) { f.Failed=true; Errors++; return; }
                if(f.Buffer.Count<size) break;
                byte[] frame=f.Buffer.GetRange(0,size).ToArray(); f.Buffer.RemoveRange(0,size);
                Frame(time,frame);
            }
        }
    }
    static void Append(Flow f,uint seq,byte[] data) {
        int skip=unchecked((int)(f.Next-seq));
        if(skip>=data.Length) return;
        for(int i=skip;i<data.Length;i++) f.Buffer.Add(data[i]);
        f.Next=unchecked(f.Next+(uint)(data.Length-skip));
    }
    public void Frame(double time,byte[] frame) {
        Frames++;
        byte[] body;
        try {
            if(frame.Length<6) { Unsupported++; return; }
            if(frame[3]==4) {
                using(var input=new MemoryStream(frame,6,frame.Length-6))
                using(var inflater=new DeflateStream(input,CompressionMode.Decompress))
                using(var output=new MemoryStream()) {
                    byte[] buffer=new byte[4096]; int count;
                    while((count=inflater.Read(buffer,0,buffer.Length))>0) {
                        output.Write(buffer,0,count);
                        if(output.Length>1048576) throw new InvalidDataException("Inflated frame limit");
                    }
                    body=output.ToArray();
                }
            } else if(frame[3]==5) {
                body=new byte[frame.Length-4];
                uint state=(uint)body.Length^522286496u;int boundary=body.Length/4*4;
                for(int i=boundary-1;i>=0;i--){state=unchecked(state+3132373u);byte key=(byte)((state>>16)&247);body[i]=(byte)(frame[i+4]^(key==0?254:key));}
                for(int i=boundary;i<body.Length;i++){state=unchecked(state+3132373u);byte key=(byte)((state>>16)&247);body[i]=(byte)(frame[i+4]^(key==0?254:key));}
                byte checksum=0;for(int i=1;i<body.Length;i++)checksum=unchecked((byte)(checksum*19+body[i]));
                if(body.Length==0||body[0]!=checksum){Errors++;return;}
            } else if(frame[3]==1) {
                body=new byte[frame.Length-4]; Array.Copy(frame,4,body,0,body.Length);
            } else { Unsupported++; return; }
            Self.Read(body);
            // Single-object type-5 doodad creation: the owner is an entity ID.
            // Capture evidence: glider 302264 -> character 80036, followed by its burn effects.
            for(int p=2;p+22<=body.Length;p++)if(body[p]==0xef&&body[p+1]==0&&body[p+2]==1&&body[p+6]==5){
                int objectId=U24(body,p+3),owner=U24(body,p+14);
                if(objectId!=0&&owner!=0&&objectId!=owner)ResolveOwner(objectId,owner);
            }
            // SCBuffCreated: preserve the item/doodad caster, target and buff instance.
            for(int p=2;p+36<=body.Length;p++)if(body[p]==0x14&&body[p+1]==3){
                int casterKind=body[p+2],caster=U24(body,p+3),targetId=U24(body,p+14),effect=BitConverter.ToInt32(body,p+21);
                uint index=BitConverter.ToUInt32(body,p+17);EffectOrigin origin=null;
                if(targetId==0||index>1000000||effect<=0||effect>1000000)continue;
                if(casterKind==1&&caster!=0&&body[p+6]==0&&body[p+7]==0&&body[p+8]==0&&body[p+9]==0&&body[p+10]==0)
                    origin=new EffectOrigin{ObjectId=caster,Time=time};
                // The glider's initial fire creates a five-second burn on the victim.
                // The secondary buff lists the victim as caster, so inherit only this verified pair.
                EffectOrigin initial;
                if(effect==33997&&casterKind==0&&caster==targetId&&gliderOrigins.TryGetValue(targetId,out initial)&&time-initial.Time>=0&&time-initial.Time<=2)
                    origin=new EffectOrigin{ObjectId=initial.ObjectId,Time=time};
                if(origin!=null){effectOrigins[EffectKey(targetId,effect,index)]=origin;if(effect==33999)gliderOrigins[targetId]=origin;}
            }
            if(OnIdentity!=null)for(int p=2;p+18<=body.Length;p++){
                if(body[p]!=2||body[p+1]!=1)continue;
                int id=U24(body,p+2),length=U16(body,p+5);if(id==0||length>160||p+18+length>body.Length)continue;
                int tail=p+7+length;if(body[tail]!=255||body[tail+1]!=255||body[tail+2]!=0)continue;
                int kind=body[tail+3];if(kind>6)continue;
                string name="";try{name=new UTF8Encoding(false,true).GetString(body,p+7,length);}catch(DecoderFallbackException){continue;}
                bool valid=true;foreach(char c in name)if(char.IsControl(c)){valid=false;break;}if(!valid)continue;
                int template=kind==1?BitConverter.ToInt32(body,tail+7):0;
                if(kind==1&&(template<=0||template>1000000))continue;
                OnIdentity(id,template,name);
            }
            // Structural candidate matching. Full bundled opcode boundaries remain unverified.
            for(int p=2;p+34<=body.Length;p++) {
                if(body[p]!=0x25 || body[p+1]!=2) continue;
                int variant=body[p+2];
                if(variant!=1 && variant!=2) continue;
                int shift=variant==2 ? -1 : 0;
                if(body[p+17+shift]!=0) continue;
                int source=U24(body,p+18+shift), repeated=U24(body,p+21+shift), target=U24(body,p+24+shift);
                if(source!=repeated || target==0) continue;
                int flags=body[p+28+shift]; if((flags & 0xc0)!=0) continue;
                int width=(flags&3)+1;
                int required=p+29+shift+width+(((flags>>2)&3)+1)+(((flags>>4)&3)+1);
                if(required>body.Length) continue;
                uint damage=0;
                for(int i=0;i<width;i++) damage|=(uint)body[p+29+shift+i]<<(i*8);
                if(damage==0 || damage>int.MaxValue) continue;
                var e=new DamageEvent {Time=time,Source=source,OriginalSource=source,Target=target,Skill=BitConverter.ToInt32(body,p+(variant==2?3:9)),Amount=(int)damage,Variant=variant,Ability=variant==1?BitConverter.ToInt32(body,p+13):0};
                EffectOrigin origin;
                if(variant==2&&effectOrigins.TryGetValue(EffectKey(target,e.Skill,BitConverter.ToUInt32(body,p+10)),out origin)&&time-origin.Time>=0&&time-origin.Time<=30){
                    int owner;if(objectOwners.TryGetValue(origin.ObjectId,out owner)){e.Source=owner;Emit(e);}else pendingOwners.Add(new KeyValuePair<int,DamageEvent>(origin.ObjectId,e));
                }else Emit(e);
            }
            // Keep unresolved events explicit instead of attributing them to the victim.
            for(int i=pendingOwners.Count-1;i>=0;i--)if(time-pendingOwners[i].Value.Time>3){var entry=pendingOwners[i];pendingOwners.RemoveAt(i);entry.Value.Source=entry.Key;Emit(entry.Value);}
        } catch(InvalidDataException) { Errors++; }
    }
}

