using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading;

// Per-session object identities; only static NPC template names are cached across sessions.
public sealed class TargetNames {
    readonly object gate=new object();
    readonly Dictionary<int,string> direct=new Dictionary<int,string>();
    readonly Dictionary<int,int> templates=new Dictionary<int,int>();
    readonly Dictionary<int,string> names=new Dictionary<int,string>();
    readonly HashSet<int> attempted=new HashSet<int>();
    readonly string cache;
    public Action Changed;
    public TargetNames(string cacheFile){cache=cacheFile;names[7512]="Легкий манекен";names[8029]="Мертвый всадник";if(File.Exists(cache))foreach(string line in File.ReadAllLines(cache)){var a=line.Split('\t');int id;if(a.Length==2&&int.TryParse(a[0],out id))names[id]=a[1];}}
    public void Observe(int id,int template,string name){bool change=false;lock(gate){
        string previous;int old;
        if(!string.IsNullOrWhiteSpace(name)){if(!direct.TryGetValue(id,out previous)||previous!=name){direct[id]=name;change=true;}templates.Remove(id);}
        else if(template>0){direct.Remove(id);if(!templates.TryGetValue(id,out old)||old!=template){templates[id]=template;change=true;}}
    }if(change&&Changed!=null)Changed();}
    public string Get(int id){lock(gate){string name;int template;if(direct.TryGetValue(id,out name))return name+" · #"+id;
        if(templates.TryGetValue(id,out template)){if(names.TryGetValue(template,out name))return name+" · #"+id;
            if(attempted.Add(template))ThreadPool.QueueUserWorkItem(delegate{Fetch(template);});
            return "NPC "+template+" · #"+id;
        }return "Имя ещё не получено · #"+id;}}
    void Fetch(int template){try{
        ServicePointManager.SecurityProtocol|=SecurityProtocolType.Tls12;
        var request=(HttpWebRequest)WebRequest.Create("https://archeagecodex.com/ru/npc/"+template+"/");request.Timeout=10000;request.ReadWriteTimeout=10000;request.UserAgent="ArcheAge-DPS/experimental";
        string html;using(var response=request.GetResponse())using(var stream=response.GetResponseStream())using(var output=new MemoryStream()){
            byte[] buffer=new byte[4096];int n;while((n=stream.Read(buffer,0,buffer.Length))>0){output.Write(buffer,0,n);if(output.Length>1048576)return;}html=Encoding.UTF8.GetString(output.ToArray());}
        var match=Regex.Match(html,@"<title>\s*(.*?)\s*-\s*Archeage Codex\s*</title>",RegexOptions.IgnoreCase|RegexOptions.Singleline);
        if(!match.Success)return;string name=WebUtility.HtmlDecode(match.Groups[1].Value).Trim();
        if(name.Length<2||name.Length>160||name.IndexOfAny(new[]{'\t','\r','\n','<','>'})>=0||name.Contains("404"))return;
        lock(gate){names[template]=name;var lines=new List<string>();foreach(var p in names)lines.Add(p.Key+"\t"+p.Value);try{File.WriteAllLines(cache,lines,Encoding.UTF8);}catch(IOException){} }
        if(Changed!=null)Changed();
    }catch(WebException){}catch(IOException){}catch(System.Security.SecurityException){} }
}
