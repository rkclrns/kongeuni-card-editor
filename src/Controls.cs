using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace KongeuniEditor
{
    sealed class NumberControl : UserControl
    {
        readonly MonoSlider slider;
        readonly TextBox input;
        int current,min,max;
        bool setting;
        public event Action<int> Changed;
        public int Value{get{return current;}set{Set(value,false);}}
        public NumberControl(int minimum,int maximum){
            min=minimum;max=maximum;Height=38;Width=285;
            slider=new MonoSlider{Minimum=min,Maximum=max,Location=new Point(0,1),Width=140,Height=34};
            SoftButton minus=new SoftButton{Text="‹",Width=29,Height=32,Anchor=AnchorStyles.Top|AnchorStyles.Right};
            SoftButton plus=new SoftButton{Text="›",Width=29,Height=32,Anchor=AnchorStyles.Top|AnchorStyles.Right};
            input=new TextBox{TextAlign=HorizontalAlignment.Center,BorderStyle=BorderStyle.FixedSingle,Width=58,Anchor=AnchorStyles.Top|AnchorStyles.Right};
            Controls.Add(slider);Controls.Add(minus);Controls.Add(input);Controls.Add(plus);
            Resize+=delegate{int x=Width-122;slider.Width=Math.Max(35,x-4);minus.Location=new Point(x,1);input.Location=new Point(x+31,3);plus.Location=new Point(Width-29,1);};
            minus.Click+=delegate{Set(current-1,true);};plus.Click+=delegate{Set(current+1,true);};
            slider.ValueChanged+=delegate{if(!setting)Set(slider.Value,true);};
            input.Leave+=delegate{Commit();};input.KeyDown+=delegate(object s,KeyEventArgs e){if(e.KeyCode==Keys.Enter){Commit();e.SuppressKeyPress=true;}if(e.KeyCode==Keys.Up){Set(current+1,true);e.SuppressKeyPress=true;}if(e.KeyCode==Keys.Down){Set(current-1,true);e.SuppressKeyPress=true;}};
            Set(min,false);
        }
        void Commit(){int v;if(Int32.TryParse(input.Text,out v))Set(v,true);else Set(current,false);}
        void Set(int value,bool notify){int next=Math.Max(min,Math.Min(max,value));bool different=current!=next;current=next;setting=true;slider.Value=next;if(input.Text!=next.ToString())input.Text=next.ToString();setting=false;if(notify&&different&&Changed!=null)Changed(next);}
    }
    sealed class Preview : Control
    {
        public readonly Card Model;
        public string SelectedId,ActiveElement;
        public Action<string> SelectionChanged,ElementSelected;
        public Action<Card> Committed;
        public Func<bool> ShiftHeld=delegate{return (Control.ModifierKeys&Keys.Shift)==Keys.Shift;};
        public float Zoom;
        public RectangleF Display;
        readonly ToolTip tip=new ToolTip{InitialDelay=300,ReshowDelay=100,AutoPopDelay=5000};
        Bitmap sampling;
        Action<Color> acceptPick;
        Action cancelPick;
        Point pickPixel,pickClient;
        public bool IsSampling{get{return sampling!=null;}}
        bool dragging,resizing,photoResize,brandResize,titleDragging,photoDragging,brandDragging;
        Card before;
        PointF down;
        float x,y,startWidth,startHeight,handleX,handleY,startScale;
        int startFont;
        RectangleF startRect;
        string dragId,hoverText="";
        RectangleF hoverBounds;
        public Preview(Card model){Model=model;DoubleBuffered=true;BackColor=Color.FromArgb(237,237,240);TabStop=true;SetStyle(ControlStyles.Selectable,true);Cursor=Cursors.Hand;}
        public void BeginPick(Action<Color> accept,Action cancel){StopPick();sampling=Model.Render();acceptPick=accept;cancelPick=cancel;Cursor=Cursors.Cross;pickPixel=new Point(Model.Width/2,Model.Height/2);pickClient=new Point(Width/2,Height/2);Focus();Invalidate();}
        public void StopPick(){if(sampling!=null){sampling.Dispose();sampling=null;}acceptPick=null;cancelPick=null;Cursor=Cursors.Hand;Invalidate();}
        public void CancelPick(){Action cancel=cancelPick;StopPick();if(cancel!=null)cancel();}
        public Color SampleColor(PointF p){if(sampling==null)throw new InvalidOperationException("스포이드가 꺼져 있어요.");return sampling.GetPixel(Math.Max(0,Math.Min(sampling.Width-1,(int)p.X)),Math.Max(0,Math.Min(sampling.Height-1,(int)p.Y)));}
        protected override void OnPaint(PaintEventArgs e){
            base.OnPaint(e);Zoom=Math.Min((Width-60f)/Model.Width,(Height-54f)/Model.Height);if(Zoom<=0)return;
            float w=Model.Width*Zoom,h=Model.Height*Zoom;Display=new RectangleF((Width-w)/2,(Height-h)/2,w,h);
            using(Brush b=new SolidBrush(Color.FromArgb(27,0,0,0)))e.Graphics.FillRectangle(b,Display.X+5,Display.Y+6,w,h);
            GraphicsState state=e.Graphics.Save();e.Graphics.TranslateTransform(Display.X,Display.Y);e.Graphics.ScaleTransform(Zoom,Zoom);e.Graphics.SetClip(new RectangleF(0,0,Model.Width,Model.Height));Model.Draw(e.Graphics);e.Graphics.Restore(state);
            if(IsSampling){DrawPicker(e.Graphics);return;}
            Bubble selected=Model.Find(SelectedId);
            if(selected!=null){
                state=e.Graphics.Save();e.Graphics.TranslateTransform(Display.X+selected.X*Zoom,Display.Y+selected.Y*Zoom);e.Graphics.RotateTransform(selected.Rotation);
                RectangleF r=Card.BubbleSelectionBounds(selected);r=new RectangleF(r.X*Zoom,r.Y*Zoom,r.Width*Zoom,r.Height*Zoom);Selection(e.Graphics,r,true);e.Graphics.Restore(state);
            }else if(ActiveElement=="제목"||ActiveElement=="캡처"||ActiveElement=="서명"||ActiveElement=="플랫폼"){
                RectangleF r=ActiveElement=="제목"?Model.TitleBounds:ActiveElement=="서명"?Model.BrandBounds:ActiveElement=="플랫폼"?Model.PlatformBounds:Model.PhotoBounds;
                Selection(e.Graphics,new RectangleF(Display.X+r.X*Zoom,Display.Y+r.Y*Zoom,r.Width*Zoom,r.Height*Zoom),ActiveElement!="제목"&&ActiveElement!="플랫폼");
            }
            if(!hoverBounds.IsEmpty&&!dragging)using(Pen p=new Pen(Color.FromArgb(150,150,160),1))e.Graphics.DrawRectangle(p,Display.X+hoverBounds.X*Zoom,Display.Y+hoverBounds.Y*Zoom,hoverBounds.Width*Zoom,hoverBounds.Height*Zoom);
            string active=selected!=null?"말풍선":ActiveElement;if(!String.IsNullOrEmpty(active))TextRenderer.DrawText(e.Graphics,"선택: "+active+(active=="제목"?" · 크기 고정":""),Font,new Point(12,5),Color.FromArgb(95,95,105));
        }
        void Selection(Graphics g,RectangleF r,bool handle){using(Pen p=new Pen(Color.FromArgb(75,75,85),1)){p.DashStyle=DashStyle.Dash;g.DrawRectangle(p,r.X,r.Y,r.Width,r.Height);}if(handle){g.FillRectangle(Brushes.White,r.Right-6,r.Bottom-6,12,12);g.DrawRectangle(Pens.DimGray,r.Right-6,r.Bottom-6,12,12);}}
        void DrawPicker(Graphics g){
            TextRenderer.DrawText(g,"스포이드 · 카드 안을 클릭 / Esc 또는 우클릭으로 취소",Font,new Point(12,5),Color.FromArgb(65,65,75));
            int bx=Math.Max(3,Math.Min(Width-136,pickClient.X+22)),by=Math.Max(30,Math.Min(Height-154,pickClient.Y+22));
            g.FillRectangle(Brushes.White,bx-3,by-3,132,151);
            for(int yy=-4;yy<=4;yy++)for(int xx=-4;xx<=4;xx++){
                int px=Math.Max(0,Math.Min(sampling.Width-1,pickPixel.X+xx)),py=Math.Max(0,Math.Min(sampling.Height-1,pickPixel.Y+yy));
                using(Brush b=new SolidBrush(sampling.GetPixel(px,py)))g.FillRectangle(b,bx+(xx+4)*14,by+(yy+4)*14,14,14);
            }
            g.DrawRectangle(Pens.Black,bx+56,by+56,14,14);Color c=sampling.GetPixel(pickPixel.X,pickPixel.Y);
            TextRenderer.DrawText(g,"#"+c.R.ToString("X2")+c.G.ToString("X2")+c.B.ToString("X2"),Font,new Point(bx,by+128),Color.FromArgb(40,40,48));
        }
        PointF Canvas(Point p){return new PointF((p.X-Display.X)/Zoom,(p.Y-Display.Y)/Zoom);}
        bool IsHandle(RectangleF r,PointF p){return Math.Abs(p.X-r.Right)<13/Zoom&&Math.Abs(p.Y-r.Bottom)<13/Zoom;}
        public string HitBubble(PointF p){for(int i=Model.Bubbles.Count-1;i>=0;i--){Bubble b=Model.Bubbles[i];PointF q=Card.ToBubblePoint(b,p);using(GraphicsPath path=Card.BubblePath(b))if(path.IsVisible(q))return b.Id;}return null;}
        protected override void OnMouseDown(MouseEventArgs e){
            base.OnMouseDown(e);if(IsSampling){if(e.Button==MouseButtons.Right){CancelPick();return;}if(e.Button==MouseButtons.Left&&Display.Contains(e.Location)){PointF pixel=Canvas(e.Location);Color color=SampleColor(pixel);Action<Color> accept=acceptPick;StopPick();if(accept!=null)accept(color);}return;}
            if(e.Button!=MouseButtons.Left||Zoom<=0)return;Focus();PointF p=Canvas(e.Location);if(!new RectangleF(0,0,Model.Width,Model.Height).Contains(p))return;
            before=Model.Snapshot();down=p;resizing=photoResize=brandResize=titleDragging=photoDragging=brandDragging=false;dragId=null;
            Bubble selected=Model.Find(SelectedId);
            if(selected!=null){RectangleF r=Card.BubbleSelectionBounds(selected);PointF q=Card.ToBubblePoint(selected,p);if(IsHandle(r,q)){resizing=true;dragId=selected.Id;startWidth=selected.Width;startHeight=selected.Height;startFont=selected.FontSize;handleX=r.Right;handleY=r.Bottom;}}
            if(!resizing&&selected==null&&ActiveElement=="캡처"&&IsHandle(Model.PhotoBounds,p)){photoResize=true;startRect=Model.PhotoBounds;startScale=Model.PhotoScale;x=Model.PhotoX;y=Model.PhotoY;}
            if(!resizing&&!photoResize&&selected==null&&ActiveElement=="서명"&&IsHandle(Model.BrandBounds,p)){brandResize=true;startRect=Model.BrandBounds;startScale=Model.BrandScale;}
            if(!resizing&&!photoResize&&!brandResize){
                string hit=HitBubble(p);SelectedId=hit;ActiveElement=hit==null?"배경":"말풍선";
                if(hit!=null){Bubble b=Model.Find(hit);dragId=hit;x=b.X;y=b.Y;}
                else if(Model.PlatformBounds.Contains(p)){ActiveElement="플랫폼";}
                else if(Model.BrandBounds.Contains(p)){brandDragging=true;ActiveElement="서명";x=Model.BrandX;y=Model.BrandY;}
                else if(Model.TitleBounds.Contains(p)){titleDragging=true;ActiveElement="제목";x=Model.TitleX;y=Model.TitleY;}
                else if(Model.PhotoBounds.Contains(p)){photoDragging=true;ActiveElement="캡처";x=Model.PhotoX;y=Model.PhotoY;}
                if(SelectionChanged!=null)SelectionChanged(hit);if(ElementSelected!=null)ElementSelected(ActiveElement);
            }
            dragging=resizing||photoResize||brandResize||dragId!=null||titleDragging||photoDragging||brandDragging;if(dragging)Capture=true;Invalidate();
        }
        static float Factor(float a,float b){return Math.Abs(a-1)>Math.Abs(b-1)?a:b;}
        protected override void OnMouseMove(MouseEventArgs e){
            base.OnMouseMove(e);if(Zoom<=0)return;PointF p=Canvas(e.Location);
            if(IsSampling){pickPixel=new Point(Math.Max(0,Math.Min(Model.Width-1,(int)p.X)),Math.Max(0,Math.Min(Model.Height-1,(int)p.Y)));pickClient=e.Location;Invalidate();return;}
            if(!dragging){
                string hit=HitBubble(p),text="";hoverBounds=RectangleF.Empty;
                if(hit!=null){Bubble found=Model.Find(hit);text="말풍선 · 클릭하면 왼쪽 설정 / Shift로 글자까지 비율 조절";hoverBounds=new RectangleF(found.X-found.Width/2,found.Y-found.Height/2,found.Width,found.Height);}
                else if(Model.PlatformBounds.Contains(p)){text=Model.PlatformLabel+" · 왼쪽 서명 탭에서 AND 지원 선택";hoverBounds=Model.PlatformBounds;}
                else if(Model.BrandBounds.Contains(p)){text="콩으니 카톡테마 · 왼쪽 서명 설정";hoverBounds=Model.BrandBounds;}
                else if(Model.TitleBounds.Contains(p)){text="제목 · 크기 고정 / 드래그로 이동";hoverBounds=Model.TitleBounds;}
                else if(Model.PhotoBounds.Contains(p)){text="캡처 · 왼쪽 설정 / 조절점으로 비율 확대";hoverBounds=Model.PhotoBounds;}
                if(hoverText!=text){hoverText=text;tip.SetToolTip(this,text);Invalidate();}return;
            }
            float dx=p.X-down.X,dy=p.Y-down.Y;Bubble b=Model.Find(dragId);
            if(resizing&&b!=null){
                PointF q=Card.ToBubblePoint(b,p);float fx=q.X/Math.Max(1,handleX),fy=q.Y/Math.Max(1,handleY);
                if(ShiftHeld()){
                    float lower=Math.Max(150/startWidth,Math.Max(75/startHeight,6f/startFont)),upper=Math.Min(1000/startWidth,Math.Min(650/startHeight,220f/startFont));
                    float factor=Math.Max(lower,Math.Min(upper,Factor(fx,fy)));b.Width=startWidth*factor;b.Height=startHeight*factor;b.FontSize=(int)Math.Round(startFont*factor);
                }else{b.Width=Math.Max(150,Math.Min(1000,startWidth*fx));b.Height=Math.Max(75,Math.Min(650,startHeight*fy));b.FontSize=startFont;}
            }else if(photoResize){float factor=Math.Max(.4f/startScale,Math.Min(1.6f/startScale,Factor((p.X-startRect.Left)/startRect.Width,(p.Y-startRect.Top)/startRect.Height)));Model.PhotoScale=startScale*factor;Model.PhotoX=x+(startRect.Width*factor-startRect.Width)/2;Model.PhotoY=y+(startRect.Height*factor-startRect.Height)/2;}
            else if(brandResize){float factor=Factor((p.X-startRect.Left)/startRect.Width,(p.Y-startRect.Top)/startRect.Height);Model.BrandScale=Math.Max(.4f,Math.Min(4,startScale*factor));}
            else if(b!=null){b.X=Math.Max(0,Math.Min(Model.Width,x+dx));b.Y=Math.Max(0,Math.Min(Model.Height,y+dy));}
            else if(titleDragging){Model.TitleX=Math.Max(-500,Math.Min(500,x+dx));Model.TitleY=Math.Max(-100,Math.Min(800,y+dy));}
            else if(photoDragging){Model.PhotoX=Math.Max(-600,Math.Min(600,x+dx));Model.PhotoY=Math.Max(-1500,Math.Min(1500,y+dy));}
            else if(brandDragging){Model.BrandX=x+dx;Model.BrandY=y+dy;}
            Invalidate();
        }
        void Finish(){if(!dragging)return;dragging=false;Card prior=before;before=null;if(Committed!=null)Committed(prior);}
        protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);Finish();Capture=false;}
        protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture)Finish();}
        protected override void OnMouseLeave(EventArgs e){hoverBounds=RectangleF.Empty;hoverText="";tip.SetToolTip(this,"");Invalidate();base.OnMouseLeave(e);}
        protected override void OnKeyDown(KeyEventArgs e){if(IsSampling&&e.KeyCode==Keys.Escape){CancelPick();e.Handled=true;}base.OnKeyDown(e);}
        protected override void Dispose(bool disposing){if(disposing){StopPick();tip.Dispose();}base.Dispose(disposing);}
    }
}
