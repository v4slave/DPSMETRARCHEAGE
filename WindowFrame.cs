using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public class ThemedWindow : Form {
    [DllImport("user32.dll")]static extern bool ReleaseCapture();
    [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr hwnd,int msg,IntPtr wp,IntPtr lp);
    readonly Panel titleBar=new Panel();
    public ThemedWindow(){
        FormBorderStyle=FormBorderStyle.None;Padding=new Padding(1,35,1,1);
        SetStyle(ControlStyles.ResizeRedraw,true);
        titleBar.BackColor=Theme.Raised;Controls.Add(titleBar);
        var title=new Label{Text="ArcheAge DPS",ForeColor=Theme.Muted,AutoSize=false,TextAlign=ContentAlignment.MiddleLeft,Left=12,Top=0,Width=230,Height=34};titleBar.Controls.Add(title);
        MouseEventHandler drag=delegate(object sender,MouseEventArgs e){if(e.Button!=MouseButtons.Left)return;ReleaseCapture();SendMessage(Handle,0xa1,new IntPtr(2),IntPtr.Zero);};
        title.MouseDown+=drag;titleBar.MouseDown+=drag;
        title.DoubleClick+=delegate{ToggleMaximize();};titleBar.DoubleClick+=delegate{ToggleMaximize();};
        var close=CaptionButton("×","Закрыть",delegate{Close();});
        var maximize=CaptionButton("□","Развернуть или восстановить",ToggleMaximize);
        var minimize=CaptionButton("−","Свернуть",delegate{WindowState=FormWindowState.Minimized;});
        titleBar.Controls.Add(minimize);titleBar.Controls.Add(maximize);titleBar.Controls.Add(close);
        titleBar.Resize+=delegate{close.Left=titleBar.Width-44;maximize.Left=titleBar.Width-88;minimize.Left=titleBar.Width-132;title.Width=Math.Max(0,titleBar.Width-150);};
    }
    Button CaptionButton(string text,string accessible,Action action){var b=new Button{Text=text,AccessibleName=accessible,Size=new Size(44,34),Top=0,FlatStyle=FlatStyle.Flat,ForeColor=Theme.Text,BackColor=Theme.Raised,Font=new Font("Segoe UI",13),TabStop=true};b.FlatAppearance.BorderSize=0;b.FlatAppearance.MouseOverBackColor=text=="×"?Color.FromArgb(145,49,49):Color.FromArgb(62,57,46);b.Click+=delegate{action();};return b;}
    void ToggleMaximize(){WindowState=WindowState==FormWindowState.Maximized?FormWindowState.Normal:FormWindowState.Maximized;}
    protected override void OnResize(EventArgs e){base.OnResize(e);if(titleBar==null)return;MaximizedBounds=Screen.FromControl(this).WorkingArea;titleBar.Bounds=new Rectangle(1,1,Math.Max(0,ClientSize.Width-2),34);titleBar.BringToFront();}
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);using(var pen=new Pen(Theme.Border))e.Graphics.DrawRectangle(pen,0,0,Width-1,Height-1);}
    protected override void WndProc(ref Message m){
        base.WndProc(ref m);
        if(m.Msg!=0x84||WindowState==FormWindowState.Maximized)return;
        long packed=m.LParam.ToInt64();Point p=PointToClient(new Point((short)(packed&65535),(short)((packed>>16)&65535)));
        bool left=p.X<6,right=p.X>=Width-6,top=p.Y<6,bottom=p.Y>=Height-6;
        int hit=top?(left?13:right?14:12):bottom?(left?16:right?17:15):left?10:right?11:0;
        if(hit!=0)m.Result=new IntPtr(hit);
    }
}
