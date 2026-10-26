using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;
using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

public static class SkillCatalog {
    public static readonly ConcurrentDictionary<int,string[]> Entries=Load();
    static readonly HashSet<int> attempted=new HashSet<int>();public static Action Changed;
    static ConcurrentDictionary<int,string[]> Load(){var result=new ConcurrentDictionary<int,string[]>();foreach(string filename in new[]{"skills.tsv","effects.tsv","extra-skills.tsv"}){string file=Path.Combine(AppPaths.Data,"assets",filename);if(File.Exists(file))foreach(string line in File.ReadAllLines(file)){var a=line.Split('\t');int id;if(a.Length>=5&&int.TryParse(a[0],out id))result[id]=a;}}return result;}
    static void Ensure(int id){if(id==0||id==int.MinValue||Entries.ContainsKey(id))return;lock(attempted){if(!attempted.Add(id))return;}ThreadPool.QueueUserWorkItem(delegate{try{
        ServicePointManager.SecurityProtocol|=SecurityProtocolType.Tls12;var request=(HttpWebRequest)WebRequest.Create("https://archeagecodex.com/ru/"+(id<0?"buff/":"skill/")+Math.Abs(id)+"/");request.Timeout=10000;request.ReadWriteTimeout=10000;
        string html;using(var response=request.GetResponse())using(var stream=response.GetResponseStream())using(var reader=new StreamReader(stream,Encoding.UTF8)){html=reader.ReadToEnd();}
        var title=Regex.Match(html,@"<title>\s*(.*?)\s*-\s*Archeage Codex\s*</title>",RegexOptions.IgnoreCase);
        var icon=Regex.Match(html,@"class=""itemwhite-icon""[^>]*>\s*<img\s+src=""[^""]*/items/([^""]+)\.png""",RegexOptions.IgnoreCase);
        if(!title.Success)return;string name=WebUtility.HtmlDecode(title.Groups[1].Value);if(name.IndexOfAny(new[]{'\t','\r','\n','<','>'})>=0)return;
        string relative="client-icons/unknown.png";
        if(icon.Success){string iconName=icon.Groups[1].Value.Replace('\\','/');
        if(Regex.IsMatch(iconName,@"^[A-Za-z0-9_/-]+$")&&!iconName.Contains("..")){
        string candidate="client-icons/"+iconName+".png";string local=Path.Combine(AppPaths.Data,"assets",candidate);
        if(!File.Exists(local))try{
            var iconRequest=(HttpWebRequest)WebRequest.Create("https://archeagecodex.com/items/"+iconName+".png");iconRequest.Timeout=10000;iconRequest.ReadWriteTimeout=10000;
            using(var response=iconRequest.GetResponse())using(var stream=response.GetResponseStream())using(var bytes=new MemoryStream()){
                byte[] buffer=new byte[4096];int n;while((n=stream.Read(buffer,0,buffer.Length))>0){bytes.Write(buffer,0,n);if(bytes.Length>2097152)return;}bytes.Position=0;
                using(var image=Image.FromStream(bytes)){Directory.CreateDirectory(Path.GetDirectoryName(local));image.Save(local,System.Drawing.Imaging.ImageFormat.Png);}
            }
        }catch(WebException){}catch(IOException){}catch(ArgumentException){}catch(System.Runtime.InteropServices.ExternalException){}
        if(File.Exists(local))relative=candidate;
        }}
        var row=new[]{id.ToString(),name,relative,"Codex",id.ToString()};Entries[id]=row;
        lock(attempted)File.AppendAllText(Path.Combine(AppPaths.Data,"assets","extra-skills.tsv"),string.Join("\t",row)+Environment.NewLine,Encoding.UTF8);
        if(Changed!=null)Changed();
    }catch(WebException){}catch(IOException){}catch(ArgumentException){}catch(System.Runtime.InteropServices.ExternalException){} });}
    public static string ImageFile(string key){int id;string[] row;return key.StartsWith("skill:")&&int.TryParse(key.Substring(6),out id)&&Entries.TryGetValue(id,out row)?row[2]:null;}
    public static string Key(DamageEvent e) {
        if(e.Variant==2 && e.Skill==21999) return "poison";
        if(e.Variant==2 && e.Skill>0){Ensure(-e.Skill);return "skill:"+(-e.Skill);}
        if(e.Variant!=2 && new[]{55540,55546,56492,56498}.Contains(e.Skill))return "quickshot";
        if(e.Variant!=2 && new[]{51998,51999,52000}.Contains(e.Skill))return "grenade";
        if(e.Variant==1 && new[]{39666,39667,39668}.Contains(e.Ability))return "skill:39666";
        if(e.Variant==1 && Entries.ContainsKey(e.Ability))return "skill:"+e.Ability;
        if(e.Variant==1&&e.Ability>0){Ensure(e.Ability);return "skill:"+e.Ability;}
        return "id:"+e.Variant+":"+e.Skill;
    }
    public static string Name(string key) {
        int id;string[] row;if(key.StartsWith("skill:")&&int.TryParse(key.Substring(6),out id)&&Entries.TryGetValue(id,out row))return row[1];
        switch(key){case "poison":return "Отравление";case "quickshot":return "Быстрый выстрел";case "grenade":return "Вирусная граната";default:return key.StartsWith("skill:-")?"Эффект #"+key.Substring(7):key.StartsWith("skill:")?"Умение #"+key.Substring(6):"Эффект "+key.Substring(3);}
    }
}
public sealed class DamageSummary {
    public List<DamageEvent> Selected;
    public long Total, Damage; public double Duration;
    public double Dps(long amount) {return Duration>0?amount/Duration:double.NaN;}
    public static DamageSummary Build(List<DamageEvent> events,int target) {
        var selection=events.Where(e=>target==0||e.Target==target).ToList();
        return new DamageSummary {
            Total=events.Sum(e=>(long)e.Amount),
            Selected=selection,
            Damage=selection.Sum(e=>(long)e.Amount),
            Duration=events.Count>1?Math.Max(0,events.Max(e=>e.Time)-events.Min(e=>e.Time)):0
        };
    }
}
public sealed class ActorDamage {
    public int Id; public long Damage; public double Share;
    public static List<ActorDamage> Build(List<DamageEvent> events,int target) {
        var rows=events.Where(e=>target==0||e.Target==target).GroupBy(e=>e.Source)
            .Select(g=>new ActorDamage{Id=g.Key,Damage=g.Sum(e=>(long)e.Amount)})
            .OrderByDescending(r=>r.Damage).ThenBy(r=>r.Id).ToList();
        long total=rows.Sum(r=>r.Damage);foreach(var row in rows)row.Share=total>0?100.0*row.Damage/total:0;
        return rows;
    }
}
class DamageChart : ScrollableControl {
    List<ActorDamage> rows=new List<ActorDamage>();Func<int,string> label;string scope="Все цели";
    public DamageChart(){Dock=DockStyle.Fill;AutoScroll=true;BackColor=Theme.Surface;SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);AccessibleName="Сравнение урона по источникам";}
    public void UpdateData(List<DamageEvent> events,int target,Func<int,string> names,string targetName){rows=ActorDamage.Build(events,target);label=names;scope=targetName;AutoScrollMinSize=new Size(0,80+rows.Count*64);AccessibleDescription=string.Join("; ",rows.Select(r=>label(r.Id)+": "+r.Damage.ToString("N0")+" урона, "+r.Share.ToString("F1")+"%"));Invalidate();}
    protected override void OnPaint(PaintEventArgs e){
        base.OnPaint(e);int offset=AutoScrollPosition.Y;
        int width=Math.Max(200,ClientSize.Width-36);
        TextRenderer.DrawText(e.Graphics,"Вклад в урон · "+scope,Font,new Rectangle(18,12+offset,width,24),Theme.Text,TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
        TextRenderer.DrawText(e.Graphics,"Все записанные источники, включая NPC и неизвестные ID",Font,new Rectangle(18,38+offset,width,24),Theme.Muted,TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
        if(rows.Count==0)TextRenderer.DrawText(e.Graphics,"Пока нет урона по выбранной цели",Font,new Rectangle(18,85+offset,width,30),Theme.Muted);
        long max=rows.Count>0?rows[0].Damage:0;
        for(int i=0;i<rows.Count;i++){
            int y=80+i*64+offset;if(y+64<0||y>ClientSize.Height)continue;
            var row=rows[i];int values=Math.Min(270,width/2);
            TextRenderer.DrawText(e.Graphics,(i+1)+". "+label(row.Id),Font,new Rectangle(18,y,width-values,25),Theme.Text,TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
            TextRenderer.DrawText(e.Graphics,row.Damage.ToString("N0")+"  ·  "+row.Share.ToString("F1")+"%",Font,new Rectangle(18+width-values,y,values,25),Theme.Text,TextFormatFlags.Right|TextFormatFlags.NoPrefix);
            using(var brush=new SolidBrush(Theme.Raised))e.Graphics.FillRectangle(brush,18,y+31,width,12);
            if(max>0)using(var brush=new SolidBrush(Theme.Gold))e.Graphics.FillRectangle(brush,18,y+31,(float)(width*(double)row.Damage/max),12);
        }
    }
}
class TargetChoice {
    public int Id;public string Label;public override string ToString(){return Label??(Id==0?"Все цели — общий урон":"Источник "+Id);}
}
class StatLabel : Label {
    public StatLabel(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.Opaque,true);}
    protected override void OnPaint(PaintEventArgs e){
        e.Graphics.Clear(BackColor);
        string[] lines=Text.Split('\n');string title=lines[0],value="—",detail=lines.Length>1?lines[1]:"";
        if(title.StartsWith("ОБЩИЙ DPS")){value=title.Substring(9).Trim();title="ОБЩИЙ DPS";}
        else{var match=Regex.Match(detail,@"DPS\s+(.*?)\s+·\s+Урон\s+(.*)");if(match.Success){value=match.Groups[1].Value;detail="Урон  "+match.Groups[2].Value;}}
        using(var small=new Font("Segoe UI",9))using(var number=new Font("Segoe UI",25,FontStyle.Bold)){
            TextRenderer.DrawText(e.Graphics,title,small,new Rectangle(14,12,Width-28,20),Theme.Muted,TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
            TextRenderer.DrawText(e.Graphics,value,number,new Rectangle(10,32,Width-20,46),Theme.Text,TextFormatFlags.NoPrefix);
            TextRenderer.DrawText(e.Graphics,detail,small,new Rectangle(14,83,Width-28,20),Theme.Gold,TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
        }
        using(var pen=new Pen(Theme.Border))e.Graphics.DrawLine(pen,0,Height-1,Width,Height-1);
    }
}
class Meter : ThemedWindow {
    readonly Color background=Theme.Background, panel=Theme.Surface, accent=Theme.Gold;
    readonly List<DamageEvent> events=new List<DamageEvent>(); readonly object gate=new object();
    readonly TargetNames names=new TargetNames(Path.Combine(AppPaths.Data,"assets","npc-names.tsv"));
    readonly Label overall=new StatLabel(), selected=new StatLabel(), status=new Label(), period=new Label();
    readonly ComboBox target=new DarkComboBox(), actor=new DarkComboBox(); readonly ListView skills=new ListView(), targets=new ListView(), hits=new ListView();
    int actorId,selfId;string selfName;
    readonly ImageList icons=new ImageList(); readonly ToolTip tips=new ToolTip();
    readonly DamageChart comparison=new DamageChart();
    CombatDecoder decoder; StreamWriter csv; Thread worker; string folder; volatile bool finished; bool closing, updating;
    int revision, displayed=-1; readonly bool replay;
    public Meter(string[] args) {
        replay=args.Length==2 && args[0]=="--replay";actorId=0;selfId=0;selfName="Ваш персонаж";
        Text="ArcheAge DPS";ClientSize=new Size(980,690);MinimumSize=new Size(840,610);
        string iconPath=Path.Combine(AppPaths.Data,"assets","application.ico");if(File.Exists(iconPath))Icon=new Icon(iconPath);
        BackColor=background;ForeColor=Theme.Text;Font=new Font("Segoe UI",10);StartPosition=FormStartPosition.CenterScreen;
        var layout=new TableLayoutPanel {Dock=DockStyle.Fill,ColumnCount=1,RowCount=5,Padding=new Padding(18)};
        foreach(int h in new[]{58,120,76,30})layout.RowStyles.Add(new RowStyle(SizeType.Absolute,h));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        var header=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false,Padding=new Padding(0,3,0,0)};
        if(Icon!=null)header.Controls.Add(new PictureBox{Image=Icon.ToBitmap(),Width=38,Height=38,SizeMode=PictureBoxSizeMode.Zoom,Margin=new Padding(0,0,12,0)});
        header.Controls.Add(new Label{Text="ARCHEAGE DPS",AutoSize=true,ForeColor=Theme.Text,Font=new Font("Segoe UI",16,FontStyle.Bold),Padding=new Padding(0,3,0,0)});
        header.Controls.Add(new Label{Text="БОЕВАЯ СТАТИСТИКА",AutoSize=true,ForeColor=Theme.Muted,Font=new Font("Segoe UI",8),Padding=new Padding(14,13,0,0)});
        layout.Controls.Add(header,0,0);
        var cards=new TableLayoutPanel {Dock=DockStyle.Fill,ColumnCount=2};cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
        foreach(var label in new[]{overall,selected}){label.Dock=DockStyle.Fill;label.BackColor=panel;label.Padding=new Padding(12);label.Font=new Font("Segoe UI",14,FontStyle.Bold);label.Margin=new Padding(0,0,10,8);}
        cards.Controls.Add(overall,0,0);cards.Controls.Add(selected,1,0);layout.Controls.Add(cards,0,1);
        var controls=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=true};
        actor.Width=260;actor.DropDownStyle=ComboBoxStyle.DropDownList;actor.AccessibleName="Источник урона — персонаж";
        actor.Items.Add(new TargetChoice{Id=0,Label="Авто: ожидается вход персонажа"});actor.SelectedIndex=0;
        actor.DropDownWidth=480;actor.SelectedIndexChanged+=delegate{if(updating)return;actorId=((TargetChoice)actor.SelectedItem).Id;displayed=-1;Render();};controls.Controls.Add(Theme.Field("ИСТОЧНИК УРОНА",actor,240));
        target.Width=300;target.DropDownStyle=ComboBoxStyle.DropDownList;target.Items.Add(new TargetChoice{Id=0});target.SelectedIndex=0;target.AccessibleName="Фильтр цели";
        target.SelectedIndexChanged+=delegate{if(!updating){displayed=-1;Render();}};
        controls.Controls.Add(Theme.Field("ЦЕЛЬ",target,285));
        var reset=new Button{Text="Новый замер",Width=125,Height=31,Margin=new Padding(0,26,12,0),FlatStyle=FlatStyle.Flat,BackColor=Theme.Raised,ForeColor=Theme.Gold};reset.FlatAppearance.BorderColor=Theme.Border;reset.FlatAppearance.MouseOverBackColor=Color.FromArgb(54,49,39);reset.Click+=delegate{lock(gate){events.Clear();revision++;}displayed=-1;Render();};controls.Controls.Add(reset);
        var pin=new CheckBox{Text="Поверх окон",AutoSize=true,Margin=new Padding(0,29,0,0)};pin.CheckedChanged+=delegate{TopMost=pin.Checked;};controls.Controls.Add(pin);
        controls.Resize+=delegate{int available=Math.Max(420,controls.ClientSize.Width-300);controls.Controls[0].Width=available*45/100;controls.Controls[1].Width=available*55/100;};
        layout.Controls.Add(controls,0,2);period.Dock=DockStyle.Fill;period.ForeColor=Theme.Muted;period.Font=new Font("Segoe UI",9);layout.Controls.Add(period,0,3);
        icons.ImageSize=new Size(32,32);icons.ColorDepth=ColorDepth.Depth32Bit;
        foreach(string name in new[]{"quickshot","grenade","poison"}) {
            string file=Path.Combine(AppPaths.Data,"assets",name+".png");
            if(File.Exists(file))using(var img=Image.FromFile(file))icons.Images.Add(name,new Bitmap(img));
        }
        // Metadata contains tens of thousands of effects. Load only icons used in displayed rows.
        var unknown=new Bitmap(32,32);using(var g=Graphics.FromImage(unknown)){g.Clear(panel);g.DrawString("?",new Font("Segoe UI",18),Brushes.Silver,5,0);}icons.Images.Add("unknown",unknown);
        skills.SmallImageList=icons;
        Configure(skills,new[]{"Умение","Урон","Доля","DPS","Попадания","Среднее"},new[]{300,145,85,120,100,120});
        Configure(targets,new[]{"Цель","Урон","Доля общего","DPS","Попадания"},new[]{290,160,145,140,120});
        Configure(hits,new[]{"Время МСК","Умение","Цель","Урон"},new[]{135,310,170,160});hits.SmallImageList=icons;
        targets.DoubleClick+=delegate{if(targets.SelectedItems.Count==1){int id=(int)targets.SelectedItems[0].Tag;for(int i=0;i<target.Items.Count;i++)if(((TargetChoice)target.Items[i]).Id==id)target.SelectedIndex=i;}};
        var tabs=Theme.Tabs();foreach(var pair in new[]{Tuple.Create("Умения",skills),Tuple.Create("Все цели",targets),Tuple.Create("Попадания",hits)}){
            var page=new TabPage(pair.Item1){BackColor=panel,Padding=new Padding(2)};page.Controls.Add(pair.Item2);tabs.TabPages.Add(page);
        }var comparisonPage=new TabPage("Сравнение"){BackColor=panel,Padding=new Padding(2)};comparisonPage.Controls.Add(comparison);tabs.TabPages.Add(comparisonPage);layout.Controls.Add(tabs,0,4);Controls.Add(layout);
        tips.SetToolTip(target,"Фильтр меняет умения, попадания и сравнение источников. Общая карточка остаётся по всем целям.");
        tips.SetToolTip(selected,"DPS по цели = её урон / общий период замера. Паузы внутри периода учитываются.");
        names.Changed=delegate{lock(gate){revision++;}};SkillCatalog.Changed=delegate{lock(gate){revision++;}};
        if(selfId>0)names.Observe(selfId,0,selfName);
        if(replay){LoadCsv(args[1]);string identityFile=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(args[1])),"identities.tsv");if(File.Exists(identityFile))foreach(string line in File.ReadAllLines(identityFile)){var a=line.Split('\t');int id,template;if(a.Length==3&&int.TryParse(a[0],out id)&&int.TryParse(a[1],out template))names.Observe(id,template,a[2]);}finished=true;}else{
            folder=args[0];Directory.CreateDirectory(folder);decoder=new CombatDecoder(0);
            csv=new StreamWriter(Path.Combine(folder,"candidate-damage.csv"));csv.AutoFlush=true;csv.WriteLine("utc,source,target,skill_candidate,damage_candidate,variant,ability,original_source");
            decoder.OnDamage=delegate(DamageEvent e){lock(gate){if(closing)return;events.Add(e);revision++;csv.WriteLine(e.Time.ToString("F6",CultureInfo.InvariantCulture)+","+e.Source+","+e.Target+","+e.Skill+","+e.Amount+","+e.Variant+","+e.Ability+","+e.OriginalSource);}};
            decoder.OnIdentity=delegate(int id,int template,string name){names.Observe(id,template,name);lock(gate){if(!closing)File.AppendAllText(Path.Combine(folder,"identities.tsv"),id+"\t"+template+"\t"+name+Environment.NewLine);}};
            decoder.Self.Changed=delegate(int id,string name){lock(gate){if(closing)return;if(id==0)events.Clear();selfId=id;selfName=name;actorId=id;revision++;}if(id>0)names.Observe(id,0,name);};
            global::Capture.AcceptPacket=new GameConnections().Contains;
            global::Capture.OnPacket=decoder.Packet;
            Shown+=delegate{worker=new Thread(delegate(){try{global::Capture.Run(new[]{args[0],args[1],args[2]});}finally{decoder.Complete();finished=true;}});worker.IsBackground=true;worker.Start();};
        }
        var timer=new System.Windows.Forms.Timer{Interval=400};timer.Tick+=delegate{Render();};timer.Start();
        FormClosing+=delegate{timer.Stop();lock(gate){closing=true;}if(folder!=null)File.WriteAllText(Path.Combine(folder,"STOP"),"");if(worker!=null)worker.Join(2000);lock(gate){if(csv!=null)csv.Dispose();}};
        FormClosed+=delegate{icons.Dispose();tips.Dispose();};Render();
    }
    void Configure(ListView view,string[] names,int[] widths){view.Dock=DockStyle.Fill;view.View=View.Details;view.FullRowSelect=true;view.HideSelection=false;view.BackColor=panel;view.ForeColor=ForeColor;view.BorderStyle=BorderStyle.None;view.ShowItemToolTips=true;for(int i=0;i<names.Length;i++)view.Columns.Add(names[i],widths[i],i==0?HorizontalAlignment.Left:HorizontalAlignment.Right);Theme.Table(view);view.Resize+=delegate{int available=Math.Max(300,view.ClientSize.Width),sum=widths.Sum();int used=0;for(int i=0;i<widths.Length;i++){int width=i==widths.Length-1?available-used:available*widths[i]/sum;view.Columns[i].Width=width;used+=width;}};}
    void LoadCsv(string path){foreach(string line in File.ReadAllLines(path).Skip(1)){if(string.IsNullOrWhiteSpace(line))continue;var a=line.Split(',');int skill=int.Parse(a[3]);events.Add(new DamageEvent{Time=double.Parse(a[0],CultureInfo.InvariantCulture),Source=int.Parse(a[1]),Target=int.Parse(a[2]),Skill=skill,Amount=int.Parse(a[4]),Variant=a.Length>5?int.Parse(a[5]):skill==21999?2:1,Ability=a.Length>6?int.Parse(a[6]):0});}revision++;}
    static string Num(double n){return double.IsNaN(n)?"—":n.ToString("N0");}
    string IconKey(string key){if(!icons.Images.ContainsKey(key)){string relative=SkillCatalog.ImageFile(key);if(relative!=null&&relative!="client-icons/unknown.png"){string file=Path.Combine(AppPaths.Data,"assets",relative);if(File.Exists(file))using(var img=Image.FromFile(file))icons.Images.Add(key,new Bitmap(img));}}return icons.Images.ContainsKey(key)?key:"unknown";}
    string ActorLabel(int id){return id==selfId?selfName+" · #"+id:names.Get(id);}
    void Render(){
        status.Text=(replay?"ЗАПИСЬ · ":finished?"ЗАХВАТ ЗАВЕРШЁН · ":"ЗАХВАТ ИДЁТ · ")+"Экспериментальный разбор; неизвестные умения показаны под ID.";
        if(decoder!=null)status.Text+="\nПакеты: "+decoder.Frames+"   Ошибки: "+decoder.Errors;
        List<DamageEvent> snapshot;int current;lock(gate){current=revision;if(displayed==current)return;snapshot=new List<DamageEvent>(events);}displayed=current;
        updating=true;var actors=snapshot.Select(e=>e.Source).Concat(selfId>0?new[]{selfId}:new int[0]).Distinct().OrderBy(id=>id).ToList();
        if(!actor.DroppedDown&&!actor.Items.Cast<TargetChoice>().Where(x=>x.Id!=0).Select(x=>x.Label).SequenceEqual(actors.Select(ActorLabel))){
            actor.Items.Clear();actor.Items.Add(new TargetChoice{Id=0,Label="Авто: ожидается вход персонажа"});foreach(int id in actors)actor.Items.Add(new TargetChoice{Id=id,Label=ActorLabel(id)});
            actor.SelectedIndex=Math.Max(0,actors.IndexOf(actorId)+1);
        }updating=false;
        var allEvents=snapshot;
        snapshot=snapshot.Where(e=>e.Source==actorId).ToList();
        if(actorId==0||snapshot.Count==0)status.Text+="\nНет ударов выбранного персонажа. Проверь список источников слева.";
        int selectedId=((TargetChoice)target.SelectedItem).Id;
        updating=true;var known=allEvents.Select(e=>e.Target).Distinct().OrderBy(i=>i).ToList();
        var existing=target.Items.Cast<TargetChoice>().Where(t=>t.Id!=0).Select(t=>t.Label).ToList();
        if(!target.DroppedDown&&!existing.SequenceEqual(known.Select(id=>names.Get(id)))){target.Items.Clear();target.Items.Add(new TargetChoice{Id=0});foreach(int id in known)target.Items.Add(new TargetChoice{Id=id,Label=names.Get(id)});target.SelectedIndex=Math.Max(0,known.IndexOf(selectedId)+1);}
        updating=false;selectedId=((TargetChoice)target.SelectedItem).Id;
        comparison.UpdateData(allEvents,selectedId,ActorLabel,selectedId==0?"Все цели":names.Get(selectedId));
        var s=DamageSummary.Build(snapshot,selectedId);
        overall.Text="ОБЩИЙ DPS   "+Num(s.Dps(s.Total))+"\nУрон  "+Num(s.Total);
        string targetLabel=selectedId==0?"ВСЕ ЦЕЛИ":names.Get(selectedId);
        selected.Text=(targetLabel.Length>38?targetLabel.Substring(0,35)+"…":targetLabel)+"\nDPS "+Num(s.Dps(s.Damage))+"   ·   Урон "+Num(s.Damage);
        tips.SetToolTip(selected,targetLabel+"\nDPS по цели = её урон / общий период замера.");
        period.Text=(replay?"Запись   ·   ":finished?"Замер завершён   ·   ":"")+s.Duration.ToString("F2")+" с   ·   "+s.Selected.Count+" попаданий   ·   "+snapshot.Select(e=>e.Target).Distinct().Count()+" целей";
        skills.BeginUpdate();skills.Items.Clear();foreach(var group in s.Selected.GroupBy(SkillCatalog.Key).OrderByDescending(g=>g.Sum(e=>(long)e.Amount))){long sum=group.Sum(e=>(long)e.Amount);var row=new ListViewItem(new[]{SkillCatalog.Name(group.Key),Num(sum),(s.Damage>0?100.0*sum/s.Damage:0).ToString("F1")+"%",Num(s.Dps(sum)),group.Count().ToString(),Num((double)sum/group.Count())},IconKey(group.Key));row.ToolTipText="Наблюдённые ID: "+string.Join(", ",group.Select(e=>e.Skill).Distinct());skills.Items.Add(row);}skills.EndUpdate();
        targets.BeginUpdate();targets.Items.Clear();foreach(var group in snapshot.GroupBy(e=>e.Target).OrderByDescending(g=>g.Sum(e=>(long)e.Amount))){long sum=group.Sum(e=>(long)e.Amount);var row=new ListViewItem(new[]{names.Get(group.Key),Num(sum),(s.Total>0?100.0*sum/s.Total:0).ToString("F1")+"%",Num(s.Dps(sum)),group.Count().ToString()});row.Tag=group.Key;targets.Items.Add(row);}targets.EndUpdate();
        hits.BeginUpdate();hits.Items.Clear();foreach(var e in s.Selected.OrderByDescending(e=>e.Time).Take(100)){string key=SkillCatalog.Key(e);hits.Items.Add(new ListViewItem(new[]{new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc).AddSeconds(e.Time).AddHours(3).ToString("HH:mm:ss.fff"),SkillCatalog.Name(key),names.Get(e.Target),Num(e.Amount)},IconKey(key)));}hits.EndUpdate();
    }
    [STAThread]static int Main(string[] args){
        bool check=args.Length==1&&args[0]=="--check-startup";bool rendering=args.Length>0&&args[0]=="--render-preview";
        try{
            AppPaths.Initialize();
            if(args.Length>=3&&args[0]=="--render-preview"){
                Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
                using(var form=new Meter(new[]{"--replay",args[1]})){
                    int source=0;
                    form.actorId=form.events.Any(e=>e.Source==source)?source:form.events.GroupBy(e=>e.Source).OrderByDescending(g=>g.Sum(e=>(long)e.Amount)).Select(g=>g.Key).FirstOrDefault();form.selfId=form.actorId;form.names.Observe(form.selfId,0,form.selfName);
                    if(args.Length>3)form.ClientSize=new Size(int.Parse(args[3]),690);
                    form.CreateControl();EnsureHandles(form);form.PerformLayout();form.displayed=-1;form.Render();
                    if(args.Length>4&&args[4]=="comparison"){var tabs=(TabControl)form.comparison.Parent.Parent;tabs.SelectedTab=(TabPage)form.comparison.Parent;form.PerformLayout();}
                    File.WriteAllText(args[2]+".layout.txt",DumpLayout(form,""));
                    using(var bitmap=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(bitmap,new Rectangle(Point.Empty,form.Size));bitmap.Save(args[2],System.Drawing.Imaging.ImageFormat.Png);}
                }return 0;
            }
            if(args.Length==0||check)args=Startup.Prepare();
            if(check){Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);using(var window=new Meter(args)){}File.WriteAllText(Path.Combine(AppPaths.Data,"startup-check.txt"),"OK: game connection, Npcap, metadata, icons and window initialization ready. No window shown.");return 0;}
            if(args.Length!=4 && !(args.Length==2&&args[0]=="--replay"))throw new ArgumentException("Неизвестные параметры запуска. Открой Meter.exe без параметров.");
            Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.Run(new Meter(args));return 0;
        }catch(Exception e){
            try{File.WriteAllText(Path.Combine(AppPaths.Data,"startup-error.log"),DateTime.UtcNow.ToString("o")+Environment.NewLine+e);}catch(IOException){}
            if(!check&&!rendering)MessageBox.Show(e.Message+"\n\nПодробности: startup-error.log","ArcheAge DPS — ошибка запуска",MessageBoxButtons.OK,MessageBoxIcon.Error);
            return 1;
        }
    }
    static void EnsureHandles(Control control){var handle=control.Handle;foreach(Control child in control.Controls)EnsureHandles(child);}
    static string DumpLayout(Control control,string indent){string s=indent+control.GetType().Name+" "+control.Bounds+" "+control.Text.Replace('\n',' ')+Environment.NewLine;foreach(Control child in control.Controls)s+=DumpLayout(child,indent+"  ");return s;}
}




