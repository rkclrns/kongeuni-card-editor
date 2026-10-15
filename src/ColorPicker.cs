using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace KongeuniEditor
{
    sealed class ColorSquare : Control
    {
        public bool HueMode;
        public double Hue,Saturation,Value=1;
        public event Action Picked;
        bool dragging;
        public ColorSquare(){DoubleBuffered=true;Cursor=Cursors.Cross;}
        protected override void OnPaint(PaintEventArgs e){
            Rectangle r=ClientRectangle;if(r.Width<1||r.Height<1)return;
            if(HueMode){for(int y=0;y<Height;y++)using(Pen p=new Pen(Hsv(y*360.0/Math.Max(1,Height-1),1,1)))e.Graphics.DrawLine(p,0,y,Width,y);int selected=(int)(Hue/360*Height);e.Graphics.DrawRectangle(Pens.White,0,selected-2,Width-1,4);}
            else{
                using(Brush b=new SolidBrush(Hsv(Hue,1,1)))e.Graphics.FillRectangle(b,r);
                using(Brush b=new LinearGradientBrush(r,Color.White,Color.FromArgb(0,255,255,255),0f))e.Graphics.FillRectangle(b,r);
                using(Brush b=new LinearGradientBrush(r,Color.FromArgb(0,0,0,0),Color.Black,90))e.Graphics.FillRectangle(b,r);
                float x=(float)Saturation*(Width-1),y=(float)(1-Value)*(Height-1);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
                using(Pen p=new Pen(Color.Black,3))e.Graphics.DrawEllipse(p,x-6,y-6,12,12);using(Pen p=new Pen(Color.White,2))e.Graphics.DrawEllipse(p,x-5,y-5,10,10);
            }
        }
        void Pick(Point p){if(HueMode)Hue=Math.Max(0,Math.Min(359.99,p.Y*360.0/Math.Max(1,Height-1)));else{Saturation=Math.Max(0,Math.Min(1,p.X/(double)Math.Max(1,Width-1)));Value=1-Math.Max(0,Math.Min(1,p.Y/(double)Math.Max(1,Height-1)));}Invalidate();if(Picked!=null)Picked();}
        protected override void OnMouseDown(MouseEventArgs e){if(e.Button!=MouseButtons.Left)return;dragging=true;Capture=true;Pick(e.Location);}
        protected override void OnMouseMove(MouseEventArgs e){if(dragging)Pick(e.Location);}
        protected override void OnMouseUp(MouseEventArgs e){dragging=false;Capture=false;}
        public static Color Hsv(double h,double s,double v){double c=v*s,x=c*(1-Math.Abs((h/60)%2-1)),m=v-c;double r=0,g=0,b=0;if(h<60){r=c;g=x;}else if(h<120){r=x;g=c;}else if(h<180){g=c;b=x;}else if(h<240){g=x;b=c;}else if(h<300){r=x;b=c;}else{r=c;b=x;}return Color.FromArgb((int)Math.Round((r+m)*255),(int)Math.Round((g+m)*255),(int)Math.Round((b+m)*255));}
    }
    sealed class ColorPicker : Form
    {
        public PaintSpec Result;
        public event Action<PaintSpec> PreviewChanged;
        public event Action<Action<Color>> SampleRequested;
        readonly ColorSquare square,hue;
        readonly TextBox hex;
        readonly NumberControl red,green,blue,angle;
        readonly Panel sample;
        readonly Label hexError;
        readonly SoftButton first,second;
        int endpoint;
        bool updating;
        public ColorPicker(string title,PaintSpec original,bool allowGradient,Card samplingSource=null){
            Result=original.Copy();Text=title;Font=Card.MakeFont(Card.FixedFont,11.25f,FontStyle.Regular,GraphicsUnit.Point);BackColor=Color.FromArgb(250,250,252);ClientSize=new Size(560,630);FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=MinimizeBox=false;StartPosition=FormStartPosition.CenterParent;
            if(Program.IsTesting){Opacity=0;ShowInTaskbar=false;}
            Controls.Add(new Label{Text=title,Font=Card.MakeFont(Card.FixedFont,15,FontStyle.Regular,GraphicsUnit.Point),AutoSize=true,Location=new Point(24,20)});
            CheckBox gradient=new CheckBox{Text="그라데이션",AutoSize=true,Location=new Point(24,67),Checked=Result.Gradient&&allowGradient,Enabled=allowGradient};Controls.Add(gradient);if(!allowGradient)Result.Gradient=false;
            first=new SoftButton{Text="시작색",Location=new Point(215,57),Size=new Size(144,40),Primary=true};second=new SoftButton{Text="끝색",Location=new Point(371,57),Size=new Size(144,40)};Controls.Add(first);Controls.Add(second);
            first.Click+=delegate{endpoint=0;UpdateFields();};second.Click+=delegate{endpoint=1;UpdateFields();};
            square=new ColorSquare{Location=new Point(24,117),Size=new Size(315,175)};hue=new ColorSquare{HueMode=true,Location=new Point(351,117),Size=new Size(26,175)};Controls.Add(square);Controls.Add(hue);
            sample=new Panel{Location=new Point(393,117),Size=new Size(142,175)};Controls.Add(sample);sample.Paint+=delegate(object s,PaintEventArgs e){using(Brush b=Result.Brush(sample.ClientRectangle))e.Graphics.FillRectangle(b,sample.ClientRectangle);};
            square.Picked+=delegate{SetColor(ColorSquare.Hsv(square.Hue,square.Saturation,square.Value));};hue.Picked+=delegate{square.Hue=hue.Hue;SetColor(ColorSquare.Hsv(hue.Hue,square.Saturation,square.Value));};
            Controls.Add(new Label{Text="HEX",Location=new Point(24,315),AutoSize=true});hex=new TextBox{Location=new Point(90,309),Size=new Size(170,30)};Controls.Add(hex);
            hexError=new Label{Text="예: #A8D8B9",Location=new Point(279,315),AutoSize=true,ForeColor=Color.Gray};Controls.Add(hexError);
            hex.TextChanged+=delegate{if(!updating&&hex.Text.Trim().TrimStart('#').Length==6)CommitHex();};hex.Leave+=delegate{CommitHex();};hex.KeyDown+=delegate(object s,KeyEventArgs e){if(e.KeyCode==Keys.Enter){CommitHex();e.SuppressKeyPress=true;}};
            red=Channel("R",355);green=Channel("G",397);blue=Channel("B",439);
            red.Changed+=delegate(int v){if(!updating){Color c=Current();SetColor(Color.FromArgb(v,c.G,c.B));}};green.Changed+=delegate(int v){if(!updating){Color c=Current();SetColor(Color.FromArgb(c.R,v,c.B));}};blue.Changed+=delegate(int v){if(!updating){Color c=Current();SetColor(Color.FromArgb(c.R,c.G,v));}};
            Controls.Add(new Label{Text="방향 (°)",Location=new Point(24,494),AutoSize=true});angle=new NumberControl(0,360){Location=new Point(135,483),Width=400,Value=Result.Angle};Controls.Add(angle);angle.Changed+=delegate(int v){if(!updating){Result.Angle=v;sample.Invalidate();if(PreviewChanged!=null)PreviewChanged(Result.Copy());}};
            SoftButton cancel=new SoftButton{Text="취소",Location=new Point(248,565),Size=new Size(132,43)},apply=new SoftButton{Text="적용",Primary=true,Location=new Point(393,565),Size=new Size(142,43)};Controls.Add(cancel);Controls.Add(apply);cancel.Click+=delegate{DialogResult=DialogResult.Cancel;Close();};apply.Click+=delegate{if(!CommitHex())return;DialogResult=DialogResult.OK;Close();};CancelButton=cancel;
            SoftButton eyedropper=new SoftButton{Text="캔버스 스포이드",Location=new Point(24,565),Size=new Size(208,43),Enabled=samplingSource!=null,AccessibleName="스포이드"};Controls.Add(eyedropper);eyedropper.Click+=delegate{if(SampleRequested!=null)SampleRequested(SetColor);};
            gradient.CheckedChanged+=delegate{Result.Gradient=gradient.Checked;if(!Result.Gradient)endpoint=0;UpdateFields();};UpdateFields();
        }
        NumberControl Channel(string name,int y){Controls.Add(new Label{Text=name,Location=new Point(24,y+8),AutoSize=true});NumberControl c=new NumberControl(0,255){Location=new Point(75,y),Width=460};Controls.Add(c);return c;}
        Color Current(){return endpoint==0?Result.First:Result.Second;}
        void SetColor(Color color){if(endpoint==0)Result.First=color;else Result.Second=color;UpdateFields();}
        bool CommitHex(){string text=hex.Text.Trim().TrimStart('#');int rgb;if(text.Length!=6||!Int32.TryParse(text,System.Globalization.NumberStyles.HexNumber,System.Globalization.CultureInfo.InvariantCulture,out rgb)){hexError.Text="6자리 HEX를 입력해 주세요";hexError.ForeColor=Color.FromArgb(180,70,70);return false;}SetColor(Color.FromArgb((rgb>>16)&255,(rgb>>8)&255,rgb&255));return true;}
        void UpdateFields(){
            updating=true;Color c=Current();red.Value=c.R;green.Value=c.G;blue.Value=c.B;hex.Text="#"+c.R.ToString("X2")+c.G.ToString("X2")+c.B.ToString("X2");hexError.Text="예: #A8D8B9";hexError.ForeColor=Color.Gray;
            if(c.GetSaturation()>0)square.Hue=hue.Hue=c.GetHue();double max=Math.Max(c.R,Math.Max(c.G,c.B))/255.0,min=Math.Min(c.R,Math.Min(c.G,c.B))/255.0;square.Value=max;square.Saturation=max==0?0:(max-min)/max;
            first.Primary=endpoint==0;second.Primary=endpoint==1;second.Enabled=Result.Gradient;angle.Enabled=Result.Gradient;angle.Value=Result.Angle;first.Invalidate();second.Invalidate();square.Invalidate();hue.Invalidate();sample.Invalidate();updating=false;if(PreviewChanged!=null)PreviewChanged(Result.Copy());
        }
    }
}
