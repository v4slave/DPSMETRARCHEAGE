using System;
using System.Text;

// Observed RU build: private character state 0x030b, then a unit/buff binding.
// An account character list or a nearby player's name cannot select the source.
public sealed class SelfIdentity {
    public Action<int,string> Changed;
    ulong character; string name; int entity;
    static int U24(byte[] b,int p){return b[p]|b[p+1]<<8|b[p+2]<<16;}
    void Bind(ulong persistent,int id){
        if(character==0||persistent!=character||id==0||entity==id)return;
        entity=id;if(Changed!=null)Changed(id,name);
    }
    public void Read(byte[] b){
        if(b.Length>=128&&b[2]==0x0b&&b[3]==3){
            int length=b[42]|b[43]<<8;
            if(length>0&&length<=96&&44+length<b.Length){
                string value=null;try{value=new UTF8Encoding(false,true).GetString(b,44,length);}catch(DecoderFallbackException){}
                bool valid=value!=null;if(valid)foreach(char c in value)if(char.IsControl(c)){valid=false;break;}
                ulong id=BitConverter.ToUInt64(b,34);
                if(valid&&id!=0){character=id;name=value;entity=0;if(Changed!=null)Changed(0,name);}
            }
        }
        for(int p=2;p+36<=b.Length;p++){
            if(b[p]==0x14&&b[p+1]==3&&b[p+2]==0)
                Bind(BitConverter.ToUInt64(b,p+6),U24(b,p+3));
            if(b[p]!=2||b[p+1]!=1)continue;
            int length=b[p+5]|b[p+6]<<8,tail=p+7+length;
            if(length>96||tail+12>b.Length||b[tail+2]!=0||b[tail+3]!=0)continue;
            Bind(BitConverter.ToUInt64(b,tail+4),U24(b,p+2));
        }
    }
}
