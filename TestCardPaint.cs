using System;
using System.Drawing;
using System.Windows.Forms;
class TestCardPaint {
 static StatLabel Make(){return new StatLabel{Size=new Size(450,90),BackColor=Color.FromArgb(27,33,44),ForeColor=Color.White,Padding=new Padding(12),Font=new Font("Segoe UI",14,FontStyle.Bold)};}
 [STAThread]static int Main(){try{
  using(var changed=Make())using(var fresh=Make())using(var oldImage=new Bitmap(450,90))using(var actual=new Bitmap(450,90))using(var expected=new Bitmap(450,90)){
   changed.Text="ВСЕ ЦЕЛИ\nDPS 5 577 · Урон 34 052";changed.DrawToBitmap(oldImage,changed.ClientRectangle);
   changed.Text="Blackholeinmyass\nDPS 664 · Урон 4 052";changed.DrawToBitmap(actual,changed.ClientRectangle);
   fresh.Text=changed.Text;fresh.DrawToBitmap(expected,fresh.ClientRectangle);
   for(int y=0;y<90;y++)for(int x=0;x<450;x++)if(actual.GetPixel(x,y)!=expected.GetPixel(x,y))throw new Exception("Card retains pixels from previous value");
  }
  Console.WriteLine("PASS: offscreen card repaint from 34052 to 4052 equals a freshly rendered card. No window displayed.");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
