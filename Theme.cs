using System;
using System.Drawing;
using System.Windows.Forms;
using System.Globalization;
public static class Theme {
    public static readonly Color Background=Color.FromArgb(18,21,27),Surface=Color.FromArgb(26,30,38),Raised=Color.FromArgb(34,39,48),Text=Color.FromArgb(239,235,224),Muted=Color.FromArgb(167,173,181),Border=Color.FromArgb(54,59,68),Gold=Color.FromArgb(216,181,115);
    public static Control Field(string caption,ComboBox box,int width){var panel=new Panel{Width=width,Height=60,Margin=new Padding(0,0,12,0)};var label=new Label{Text=caption,Dock=DockStyle.Top,Height=22,ForeColor=Muted,Font=new Font("Segoe UI",8,FontStyle.Bold)};box.Dock=DockStyle.Bottom;box.Height=32;box.FlatStyle=FlatStyle.Flat;box.BackColor=Raised;box.ForeColor=Text;box.Font=new Font("Segoe UI",10);panel.Controls.Add(box);panel.Controls.Add(label);var face=new ComboFace(box);panel.Controls.Add(face);panel.Layout+=delegate{face.Bounds=new Rectangle(0,panel.ClientSize.Height-box.Height,panel.ClientSize.Width,box.Height);face.BringToFront();};return panel;}
    public static TabControl Tabs(){return new DarkTabs{Dock=DockStyle.Fill,ItemSize=new Size(170,37),SizeMode=TabSizeMode.Fixed,Padding=new Point(14,8)};}
    public static void Table(ListView view){
        view.OwnerDraw=true;view.GridLines=false;
        view.DrawColumnHeader+=delegate(object sender,DrawListViewColumnHeaderEventArgs e){using(var brush=new SolidBrush(Raised))e.Graphics.FillRectangle(brush,e.Bounds);var rect=e.Bounds;rect.Inflate(-12,0);TextRenderer.DrawText(e.Graphics,e.Header.Text,new Font("Segoe UI",9),rect,Muted,TextFormatFlags.VerticalCenter|(e.ColumnIndex==0?TextFormatFlags.Left:TextFormatFlags.Right));};
        view.DrawItem+=delegate(object sender,DrawListViewItemEventArgs e){};
        view.DrawSubItem+=delegate(object sender,DrawListViewSubItemEventArgs e){
            Color background=e.Item.Selected?Color.FromArgb(60,55,43):e.ItemIndex%2==0?Surface:Color.FromArgb(29,33,41);
            using(var brush=new SolidBrush(background))e.Graphics.FillRectangle(brush,e.Bounds);
            var rect=e.Bounds;rect.Inflate(-12,0);
            if(e.ColumnIndex==0&&view.SmallImageList!=null&&view.SmallImageList.Images.ContainsKey(e.Item.ImageKey)){var box=new Rectangle(e.Bounds.Left+9,e.Bounds.Top+4,28,28);e.Graphics.DrawImage(view.SmallImageList.Images[e.Item.ImageKey],box);rect.X+=34;rect.Width-=34;}
            TextRenderer.DrawText(e.Graphics,e.SubItem.Text,view.Font,rect,e.ColumnIndex==1?Text:Theme.Text,TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix|(e.ColumnIndex==0?TextFormatFlags.Left:TextFormatFlags.Right));
            if(view.Columns[e.ColumnIndex].Text.StartsWith("Доля")){double pct;if(double.TryParse(e.SubItem.Text.Replace("%", ""),NumberStyles.Float,CultureInfo.CurrentCulture,out pct)){var bar=new Rectangle(rect.X,e.Bounds.Bottom-5,(int)(rect.Width*Math.Min(100,Math.Max(0,pct))/100),2);using(var brush=new SolidBrush(Gold))e.Graphics.FillRectangle(brush,bar);}}
        };
    }
}
public class ComboFace : Control {
    readonly ComboBox box;
    public ComboFace(ComboBox owner){box=owner;SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);TabStop=false;Cursor=Cursors.Hand;box.SelectedIndexChanged+=delegate{Invalidate();};box.GotFocus+=delegate{Invalidate();};box.LostFocus+=delegate{Invalidate();};}
    protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button==MouseButtons.Left){box.Focus();box.DroppedDown=!box.DroppedDown;}}
    protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(Theme.Raised);using(var pen=new Pen(box.Focused?Theme.Gold:Theme.Border))e.Graphics.DrawRectangle(pen,0,0,Width-1,Height-1);string text=box.SelectedIndex>=0?box.GetItemText(box.SelectedItem):box.Text;TextRenderer.DrawText(e.Graphics,text,box.Font,new Rectangle(8,0,Math.Max(0,Width-33),Height),Theme.Text,TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);using(var pen=new Pen(Theme.Muted,1.5f)){int x=Width-16,y=Height/2;e.Graphics.DrawLines(pen,new[]{new Point(x-4,y-2),new Point(x,y+2),new Point(x+4,y-2)});}}
}
public class DarkTabs : TabControl {
    public DarkTabs(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);}
    protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(Theme.Background);for(int i=0;i<TabCount;i++){
        Rectangle rect=GetTabRect(i);bool active=i==SelectedIndex;
        using(var brush=new SolidBrush(active?Theme.Raised:Theme.Background))e.Graphics.FillRectangle(brush,rect);
        if(active)using(var pen=new Pen(Theme.Gold,2))e.Graphics.DrawLine(pen,rect.Left+10,rect.Bottom-2,rect.Right-10,rect.Bottom-2);
        using(var font=new Font("Segoe UI",10,active?FontStyle.Bold:FontStyle.Regular))TextRenderer.DrawText(e.Graphics,TabPages[i].Text,font,rect,active?Theme.Text:Theme.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
    }}
}
public class DarkComboBox : ComboBox {
    public DarkComboBox(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);DrawMode=DrawMode.OwnerDrawFixed;ItemHeight=24;BackColor=Theme.Raised;ForeColor=Theme.Text;FlatStyle=FlatStyle.Flat;}
    protected override void OnPaint(PaintEventArgs e){
        e.Graphics.Clear(Theme.Raised);using(var pen=new Pen(Focused?Theme.Gold:Theme.Border))e.Graphics.DrawRectangle(pen,0,0,Width-1,Height-1);
        string text=SelectedIndex>=0?GetItemText(SelectedItem):Text;
        TextRenderer.DrawText(e.Graphics,text,Font,new Rectangle(8,0,Math.Max(0,Width-33),Height),Theme.Text,TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
        using(var pen=new Pen(Theme.Muted,1.5f)){int x=Width-16,y=Height/2;e.Graphics.DrawLines(pen,new[]{new Point(x-4,y-2),new Point(x,y+2),new Point(x+4,y-2)});}
    }
    protected override void OnSelectedIndexChanged(EventArgs e){base.OnSelectedIndexChanged(e);Invalidate();}
    protected override void WndProc(ref Message m){
        if((m.Msg==0x317||m.Msg==0x318)&&m.WParam!=IntPtr.Zero){using(var graphics=Graphics.FromHdc(m.WParam))OnPaint(new PaintEventArgs(graphics,ClientRectangle));return;}
        base.WndProc(ref m);
    }
    protected override void OnDrawItem(DrawItemEventArgs e){
        using(var brush=new SolidBrush((e.State&DrawItemState.Selected)!=0?Color.FromArgb(61,55,44):Theme.Raised))e.Graphics.FillRectangle(brush,e.Bounds);
        string text=e.Index>=0&&e.Index<Items.Count?GetItemText(Items[e.Index]):Text;
        Rectangle rect=e.Bounds;rect.Inflate(-7,0);TextRenderer.DrawText(e.Graphics,text,Font,rect,Theme.Text,TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
        if((e.State&DrawItemState.Focus)!=0)e.DrawFocusRectangle();
    }
}

