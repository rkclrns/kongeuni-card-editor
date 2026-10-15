using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
namespace KongeuniEditor
{
    static class WheelRouting
    {
        public static void Scroll(Control source,int delta){for(Control p=source.Parent;p!=null;p=p.Parent){ScrollableControl scroll=p as ScrollableControl;if(scroll!=null&&scroll.AutoScroll&&scroll.VerticalScroll.Visible){scroll.AutoScrollPosition=new Point(0,Math.Max(0,-scroll.AutoScrollPosition.Y-delta/120*58));return;}}}
    }
    sealed class QuietComboBox : ComboBox
    {
        protected override void OnMouseWheel(MouseEventArgs e){WheelRouting.Scroll(this,e.Delta);}
        protected override void WndProc(ref Message m){if(m.Msg==0x20A){WheelRouting.Scroll(this,(short)((m.WParam.ToInt64()>>16)&65535));return;}base.WndProc(ref m);}
    }
    sealed class MonoSlider : Control
    {
        int minimum,maximum=100,current;
        bool dragging,hover;
        public event EventHandler ValueChanged;
        public int Minimum{get{return minimum;}set{minimum=value;}}
        public int Maximum{get{return maximum;}set{maximum=value;}}
        public int Value{get{return current;}set{int next=Math.Max(minimum,Math.Min(maximum,value));if(next==current)return;current=next;Invalidate();if(ValueChanged!=null)ValueChanged(this,EventArgs.Empty);}}
        public MonoSlider(){SetStyle(ControlStyles.Selectable|ControlStyles.OptimizedDoubleBuffer|ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint,true);TabStop=true;Cursor=Cursors.Hand;Height=34;}
        protected override void OnPaint(PaintEventArgs e){e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;float y=Height/2f,x=9+(Width-18)*(current-minimum)/(float)Math.Max(1,maximum-minimum);using(Pen p=new Pen(Color.FromArgb(217,217,222),4)){p.StartCap=p.EndCap=LineCap.Round;e.Graphics.DrawLine(p,9,y,Width-9,y);}using(Pen p=new Pen(Enabled?Color.FromArgb(101,101,111):Color.FromArgb(202,202,207),4)){p.StartCap=p.EndCap=LineCap.Round;e.Graphics.DrawLine(p,9,y,x,y);}using(Brush b=new SolidBrush(Enabled?Color.FromArgb(62,62,70):Color.FromArgb(195,195,201)))e.Graphics.FillEllipse(b,x-6,y-6,12,12);if((hover||Focused)&&Enabled)using(Pen p=new Pen(Color.FromArgb(145,145,153),1))e.Graphics.DrawEllipse(p,x-9,y-9,18,18);}
        void Pick(int x){Value=minimum+(int)Math.Round(Math.Max(0,Math.Min(1,(x-9)/(double)Math.Max(1,Width-18)))*(maximum-minimum));}
        protected override void OnMouseDown(MouseEventArgs e){if(e.Button!=MouseButtons.Left)return;Focus();Capture=true;dragging=true;Pick(e.X);}
        protected override void OnMouseMove(MouseEventArgs e){if(dragging)Pick(e.X);}
        protected override void OnMouseUp(MouseEventArgs e){dragging=false;Capture=false;}
        protected override void OnMouseWheel(MouseEventArgs e){WheelRouting.Scroll(this,e.Delta);}
        protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
        protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
        protected override void OnGotFocus(EventArgs e){Invalidate();base.OnGotFocus(e);}
        protected override void OnLostFocus(EventArgs e){Invalidate();base.OnLostFocus(e);}
        protected override bool IsInputKey(Keys keyData){return keyData==Keys.Left||keyData==Keys.Right||base.IsInputKey(keyData);}
        protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.Left||e.KeyCode==Keys.Down){Value--;e.Handled=true;}if(e.KeyCode==Keys.Right||e.KeyCode==Keys.Up){Value++;e.Handled=true;}base.OnKeyDown(e);}
    }
    sealed class ColorChip : Control
    {
        public PaintSpec FillSpec;
        bool hover;
        public ColorChip(){DoubleBuffered=true;Size=new Size(43,32);Cursor=Cursors.Hand;TabStop=true;}
        protected override void OnPaint(PaintEventArgs e){e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;RectangleF r=new RectangleF(3,3,Width-7,Height-7);using(GraphicsPath path=Card.RoundPath(r,5)){using(Brush b=(FillSpec??new PaintSpec(Color.White,Color.White)).Brush(r))e.Graphics.FillPath(b,path);using(Pen p=new Pen(hover||Focused?Color.FromArgb(50,50,58):Color.FromArgb(166,166,176),hover||Focused?2:1))e.Graphics.DrawPath(p,path);}}
        protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
        protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
        protected override void OnGotFocus(EventArgs e){Invalidate();base.OnGotFocus(e);}
        protected override void OnLostFocus(EventArgs e){Invalidate();base.OnLostFocus(e);}
        protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.Enter||e.KeyCode==Keys.Space){OnClick(EventArgs.Empty);e.Handled=true;}base.OnKeyDown(e);}
    }
    sealed class FontPicker : Form
    {
        public string Result;
        public event Action<string> PreviewChanged;
        readonly ListBox list;
        readonly TextBox search;
        readonly Panel sample;
        readonly List<string> fonts=new List<string>();
        readonly Dictionary<string,Font> sampleFonts=new Dictionary<string,Font>();
        readonly string text;
        bool updating;
        public FontPicker(string current,string previewText){
            Result=current;text=previewText;Text="폰트 미리보기";Font=Card.MakeFont(Card.FixedFont,11.25f,FontStyle.Regular,GraphicsUnit.Point);BackColor=Color.FromArgb(250,250,252);ClientSize=new Size(620,690);StartPosition=FormStartPosition.CenterParent;MinimizeBox=MaximizeBox=false;FormBorderStyle=FormBorderStyle.FixedDialog;if(Program.IsTesting){Opacity=0;ShowInTaskbar=false;}
            Controls.Add(new Label{Text="폰트 이름 검색 · 클릭하면 캔버스에 미리보기",Location=new Point(22,18),AutoSize=true});search=new TextBox{Location=new Point(22,53),Width=576};Controls.Add(search);
            list=new ListBox{Location=new Point(22,96),Size=new Size(576,358),DrawMode=DrawMode.OwnerDrawFixed,ItemHeight=50,BorderStyle=BorderStyle.None,IntegralHeight=false};Controls.Add(list);
            using(InstalledFontCollection f=new InstalledFontCollection())foreach(FontFamily family in f.Families)if(!fonts.Contains(family.Name))fonts.Add(family.Name);foreach(string name in AppFonts.Names)if(!fonts.Contains(name))fonts.Add(name);fonts.Sort(StringComparer.CurrentCultureIgnoreCase);
            list.DrawItem+=delegate(object sender,DrawItemEventArgs e){if(e.Index<0)return;string name=list.Items[e.Index].ToString();Color background=(e.State&DrawItemState.Selected)!=0?Color.FromArgb(226,226,233):Color.White;using(Brush b=new SolidBrush(background))e.Graphics.FillRectangle(b,e.Bounds);TextRenderer.DrawText(e.Graphics,name,Font,new Rectangle(e.Bounds.X+10,e.Bounds.Y+5,e.Bounds.Width-20,20),Color.FromArgb(55,55,63),TextFormatFlags.EndEllipsis);if(!sampleFonts.ContainsKey(name))sampleFonts[name]=Card.MakeFont(name,17,FontStyle.Regular,GraphicsUnit.Pixel);TextRenderer.DrawText(e.Graphics,"콩으니 카톡테마 Aa",sampleFonts[name],new Rectangle(e.Bounds.X+10,e.Bounds.Y+26,e.Bounds.Width-20,23),Color.FromArgb(90,90,100),TextFormatFlags.EndEllipsis);};
            sample=new Panel{Location=new Point(22,469),Size=new Size(576,132),BackColor=Color.White};Controls.Add(sample);sample.Paint+=delegate(object s,PaintEventArgs e){using(Font f=Card.MakeFont(Result,30,FontStyle.Regular,GraphicsUnit.Pixel))using(Brush b=new SolidBrush(Color.FromArgb(40,40,48)))using(StringFormat fmt=new StringFormat()){fmt.Alignment=StringAlignment.Center;fmt.LineAlignment=StringAlignment.Center;e.Graphics.DrawString(text,f,b,new RectangleF(12,8,sample.Width-24,sample.Height-16),fmt);}};
            list.SelectedIndexChanged+=delegate{if(updating||list.SelectedItem==null)return;Result=list.SelectedItem.ToString();sample.Invalidate();if(PreviewChanged!=null)PreviewChanged(Result);};
            search.TextChanged+=delegate{Filter();};SoftButton cancel=new SoftButton{Text="취소",Location=new Point(308,626),Size=new Size(135,42)},apply=new SoftButton{Text="적용",Primary=true,Location=new Point(455,626),Size=new Size(143,42)};Controls.Add(cancel);Controls.Add(apply);cancel.Click+=delegate{DialogResult=DialogResult.Cancel;Close();};apply.Click+=delegate{DialogResult=DialogResult.OK;Close();};CancelButton=cancel;
            Filter();FormClosed+=delegate{foreach(Font f in sampleFonts.Values)f.Dispose();};
        }
        void Filter(){updating=true;list.BeginUpdate();list.Items.Clear();foreach(string name in fonts)if(name.IndexOf(search.Text,StringComparison.CurrentCultureIgnoreCase)>=0)list.Items.Add(name);list.SelectedItem=Result;list.EndUpdate();updating=false;}
    }
}
