using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
namespace KongeuniEditor
{
    public sealed partial class Card
    {
        public void Draw(Graphics g){
            g.SmoothingMode=SmoothingMode.AntiAlias;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;g.TextRenderingHint=TextRenderingHint.AntiAliasGridFit;
            RectangleF canvas=new RectangleF(0,0,Width,Height);
            using(Brush b=BackgroundFill.Brush(canvas))g.FillRectangle(b,canvas);
            if(Pattern>0){
                using(Brush pattern=PatternFill.Brush(canvas)){
                    // Alpha is applied to both gradient endpoints independently of their hue.
                    PaintSpec fade=PatternFill.Copy();fade.First=Color.FromArgb(PatternOpacity*255/100,fade.First);fade.Second=Color.FromArgb(PatternOpacity*255/100,fade.Second);
                    using(Brush b=fade.Brush(canvas)){
                        if(Pattern==1)using(Pen p=new Pen(b,2)){for(int x=0;x<Width;x+=PatternSize)g.DrawLine(p,x,0,x,Height);for(int y=0;y<Height;y+=PatternSize)g.DrawLine(p,0,y,Width,y);}
                        if(Pattern>=2){int row=0;float vertical=PatternSize*DotRowSpacing/100f;for(float y=PatternSize/2f;y<Height;y+=vertical,row++){float start=PatternSize/2f+(row%2==0?0:PatternSize/2f);for(float x=start;x<Width;x+=PatternSize){if(Pattern==2)g.FillEllipse(b,x-DotSize/2f,y-DotSize/2f,DotSize,DotSize);else using(GraphicsPath motif=PatternMotif(Pattern,x,y,DotSize))g.FillPath(b,motif);}}}
                    }
                }
            }
            BrandBounds=Letter(g,"콩으니 카톡테마",FixedFont,29*BrandScale,new RectangleF(64+BrandX,48+BrandY,500,47*BrandScale),BrandFill,BrandStyle,false);BrandBounds.Inflate(BrandStyle.Outline?BrandStroke/2f+4:4,8);
            PlatformBounds=Letter(g,PlatformLabel,FixedFont,29*BrandScale,new RectangleF(Width-464,48,400,47*BrandScale),BrandFill,BrandStyle,false,true);PlatformBounds.Inflate(BrandStyle.Outline?BrandStroke/2f+4:4,8);
            float baseTitleY=TitleAtBottom?Height-234*TitleSize-125:126;
            float titleY=baseTitleY+TitleY;
            float photoTop=TitleAtBottom?135:410+(Height>1600?65:0),available=TitleAtBottom?Math.Max(80,baseTitleY-45-photoTop):Height-photoTop-95;
            float fit=Math.Min(740f/Photo.Width,available/Photo.Height)*PhotoScale;
            float pw=Photo.Width*fit,ph=Photo.Height*fit;
            float px=(Width-pw)/2+PhotoX,py=photoTop+(available-ph)/2+PhotoY;
            PhotoBounds=new RectangleF(px,py,pw,ph);
            if(PhotoShadow){int margin=Frame?FrameWidth:0;using(Brush b=new SolidBrush(Color.FromArgb(38,0,0,0)))Round(g,b,new RectangleF(px-margin+7,py-margin+11,pw+margin*2,ph+margin*2),16);}
            if(Frame&&FrameWidth>0){using(Brush b=new SolidBrush(FrameColor))Round(g,b,new RectangleF(px-FrameWidth,py-FrameWidth,pw+FrameWidth*2,ph+FrameWidth*2),14);}
            g.DrawImage(Photo,PhotoBounds);
            string title=String.IsNullOrWhiteSpace(Name)?"나의 테마":Name.Trim();
            RectangleF first=Letter(g,title,CardFontName,85*TitleSize,new RectangleF(55+TitleX,titleY,970,108*TitleSize),TitleFill,TitleStyle,true);
            RectangleF second=Letter(g,"카톡테마 공유",CardFontName,91*TitleSize,new RectangleF(55+TitleX,titleY+117*TitleSize,970,117*TitleSize),TitleFill,TitleStyle,true);
            TitleGlyphBounds=RectangleF.Union(first,second);TitleBounds=TitleGlyphBounds;TitleBounds.Inflate(TitleStroke/2f+8,TitleStroke/2f+8);
            foreach(Bubble bubble in Bubbles)DrawBubble(g,bubble);
            if(!String.IsNullOrWhiteSpace(Account))TextWithFont(g,Account,25,Ink,new RectangleF(60,Height-60,960,40),false,true,FixedFont);
        }
        public static GraphicsPath PatternMotif(int kind,float x,float y,float size){
            GraphicsPath p=new GraphicsPath();float r=size/2;
            if(kind==3){PointF[] points=new PointF[10];for(int i=0;i<10;i++){double a=(i*36-90)*Math.PI/180;float radius=i%2==0?r:r*.47f;points[i]=new PointF(x+(float)Math.Cos(a)*radius,y+(float)Math.Sin(a)*radius);}p.AddPolygon(points);}
            else{p.AddBezier(0,size*.45f,-size*.2f,size*.30f,-size*.58f,-size*.02f,-size*.45f,-size*.28f);p.AddBezier(-size*.45f,-size*.28f,-size*.32f,-size*.52f,-size*.10f,-size*.48f,0,-size*.22f);p.AddBezier(0,-size*.22f,size*.10f,-size*.48f,size*.32f,-size*.52f,size*.45f,-size*.28f);p.AddBezier(size*.45f,-size*.28f,size*.58f,-size*.02f,size*.2f,size*.30f,0,size*.45f);p.CloseFigure();using(Matrix m=new Matrix()){m.Translate(x,y);p.Transform(m);}}
            return p;
        }
        public static RectangleF Letter(Graphics g,string text,string family,float size,RectangleF area,PaintSpec fill,TextLook look,bool center,bool right=false){
            if(String.IsNullOrEmpty(text))return RectangleF.Empty;
            FontStyle desired=(look.Bold?FontStyle.Bold:FontStyle.Regular)|(look.Italic?FontStyle.Italic:FontStyle.Regular);
            string candidate=look.Bold&&family.EndsWith(" Regular")?family.Substring(0,family.Length-8)+" Bold":!look.Bold&&family.EndsWith(" Bold")?family.Substring(0,family.Length-5)+" Regular":family;
            if(candidate!=family)try{using(FontFamily available=AppFonts.Family(candidate))family=available.Name;}catch(ArgumentException){}
            using(Font font=MakeFont(family,size,desired,GraphicsUnit.Pixel))using(GraphicsPath path=new GraphicsPath())using(StringFormat fmt=(StringFormat)StringFormat.GenericTypographic.Clone()){
                path.AddString(text,font.FontFamily,(int)font.Style,size,new PointF(0,0),fmt);
                if(look.Italic&&!font.Italic)using(Matrix italic=new Matrix(1,0,-.20f,1,0,0))path.Transform(italic);
                RectangleF bounds=path.GetBounds();if(bounds.Width<=0||bounds.Height<=0)return RectangleF.Empty;
                using(Matrix m=new Matrix()){m.Translate(-bounds.X,-bounds.Y);path.Transform(m);}
                float x=right?area.Right-bounds.Width:center?area.X+(area.Width-bounds.Width)/2:area.X,y=area.Y+(area.Height-bounds.Height)/2;
                using(Matrix m=new Matrix()){m.Translate(x,y);path.Transform(m);}
                int stroke=look.Outline?look.Stroke:0;
                if(look.Glow&&look.GlowOpacity>0){
                    for(int radius=look.GlowRadius;radius>=1;radius-=2){
                        int alpha=Math.Min(255,Math.Max(1,(int)(look.GlowOpacity*(1-radius/(float)(look.GlowRadius+1))*.30f)));
                        using(Pen p=new Pen(Color.FromArgb(alpha,look.GlowColor),stroke+radius*2)){p.LineJoin=LineJoin.Round;g.DrawPath(p,path);}
                    }
                }
                if(look.Shadow&&look.ShadowOpacity>0)using(GraphicsPath shifted=(GraphicsPath)path.Clone())using(Matrix m=new Matrix()){
                    m.Translate(look.ShadowDistance,look.ShadowDistance);shifted.Transform(m);
                    using(Brush b=new SolidBrush(Color.FromArgb(look.ShadowOpacity,0,0,0)))using(Pen p=new Pen(b,Math.Max(1,stroke))){p.LineJoin=LineJoin.Round;if(stroke>0)g.DrawPath(p,shifted);g.FillPath(b,shifted);}
                }
                if(stroke>0)using(Pen p=new Pen(look.OutlineColor,stroke)){p.LineJoin=LineJoin.Round;g.DrawPath(p,path);}
                using(Brush b=fill.Brush(area)){g.FillPath(b,path);if(look.Bold&&!font.Bold)using(Pen p=new Pen(b,Math.Max(1,size*.025f))){p.LineJoin=LineJoin.Round;g.DrawPath(p,path);}}
                return path.GetBounds();
            }
        }
        public static GraphicsPath RoundPath(RectangleF r,float radius){GraphicsPath p=new GraphicsPath();float d=Math.Max(1,Math.Min(radius*2,Math.Min(r.Width,r.Height)));p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
        public static void Round(Graphics g,Brush b,RectangleF r,float radius){using(GraphicsPath p=RoundPath(r,radius))g.FillPath(b,p);}
        public static GraphicsPath BubblePath(Bubble b){
            float w=b.Width,h=b.Height,x=-w/2,y=-h/2;
            GraphicsPath p=new GraphicsPath();
            if(b.Kind==0){
                float r=Math.Min(34,h*.44f),d=r*2;
                p.AddArc(x,y,d,d,180,90);p.AddArc(x+w-d,y,d,d,270,90);p.AddArc(x+w-d,y+h-d,d,d,0,90);
                p.AddLine(x+w-r,y+h,x+w*.73f,y+h);p.AddBezier(x+w*.73f,y+h,x+w*.75f,y+h+9,x+w*.83f,y+h+24,x+w*.81f,y+h+24);
                p.AddLine(x+w*.81f,y+h+24,x+w*.57f,y+h);p.AddLine(x+w*.57f,y+h,x+r,y+h);p.AddArc(x,y+h-d,d,d,90,90);p.CloseFigure();
            }else if(b.Kind==1){
                p.AddBezier(0,y,w*.7f,y,w*.7f,h*.31f,w*.21f,h*.44f);p.AddLine(w*.21f,h*.44f,w*.35f,h*.68f);p.AddLine(w*.35f,h*.68f,w*.04f,h*.50f);
                p.AddBezier(w*.04f,h*.50f,-w*.7f,h*.6f,-w*.7f,y,0,y);p.CloseFigure();
            }else if(b.Kind==2){
                p.AddBezier(.14f,.74f,-.08f,.72f,-.04f,.28f,.13f,.27f);
                p.AddBezier(.13f,.27f,.17f,.08f,.30f,.04f,.38f,.12f);
                p.AddBezier(.38f,.12f,.47f,-.06f,.58f,-.02f,.65f,.10f);
                p.AddBezier(.65f,.10f,.79f,-.01f,.91f,.14f,.92f,.29f);
                p.AddBezier(.92f,.29f,1.12f,.44f,1.00f,.73f,.83f,.75f);
                p.AddBezier(.83f,.75f,.79f,.96f,.65f,.98f,.57f,.88f);
                p.AddBezier(.57f,.88f,.43f,1.10f,.30f,1.01f,.25f,.88f);
                p.AddBezier(.25f,.88f,.17f,.89f,.13f,.82f,.14f,.74f);p.CloseFigure();
                using(Matrix m=new Matrix(w,0,0,h,-w/2,-h/2))p.Transform(m);
                float radius=h*.10f;p.AddEllipse(-w*.20f-radius,h*.73f-radius,radius*2,radius*2);
                radius=h*.065f;p.AddEllipse(-w*.32f-radius,h*1.05f-radius,radius*2,radius*2);
            }else if(b.Kind==3){
                PointF[] pts=new PointF[28];for(int i=0;i<28;i++){double a=(i*360.0/28-90)*Math.PI/180;float r=i%2==0?1.08f:.82f;pts[i]=new PointF((float)Math.Cos(a)*w/2*r,(float)Math.Sin(a)*h/2*r);}p.AddPolygon(pts);
            }else if(b.Kind==4){
                p.AddPolygon(new PointF[]{new PointF(x,y),new PointF(x+w,y),new PointF(x+w-30,0),new PointF(x+w,y+h),new PointF(x,y+h)});
            }else if(b.Kind==5)p.AddRectangle(new RectangleF(x,y,w,h));
            else p.AddEllipse(x,y,w,h);
            if(b.Mirrored)using(Matrix m=new Matrix(-1,0,0,1,0,0))p.Transform(m);
            return p;
        }
        public static RectangleF BubbleSelectionBounds(Bubble bubble){using(GraphicsPath path=BubblePath(bubble)){RectangleF r=path.GetBounds();r.Inflate(8,8);return r;}}
        static void DrawBubble(Graphics g,Bubble bubble){
            GraphicsState state=g.Save();g.TranslateTransform(bubble.X,bubble.Y);g.RotateTransform(bubble.Rotation);
            using(GraphicsPath path=BubblePath(bubble)){
                if(bubble.Shadow)using(GraphicsPath shadow=(GraphicsPath)path.Clone())using(Matrix m=new Matrix()){m.Translate(5,7);shadow.Transform(m);using(Brush b=new SolidBrush(Color.FromArgb(50,0,0,0)))g.FillPath(b,shadow);}
                using(Brush b=new SolidBrush(bubble.Fill))g.FillPath(b,path);
                if(bubble.Border>0)using(Pen p=new Pen(bubble.Line,bubble.Border)){p.LineJoin=LineJoin.Round;g.DrawPath(p,path);}
            }
            using(Font f=MakeFont(bubble.FontName,bubble.FontSize,FontStyle.Regular,GraphicsUnit.Pixel)){
                string[] lines=bubble.Text.Replace("\r","").Split('\n');float lineHeight=f.GetHeight(g),top=-lines.Length*lineHeight/2;
                PaintSpec fill=new PaintSpec(bubble.Ink,bubble.Ink);
                for(int i=0;i<lines.Length;i++)Letter(g,lines[i],bubble.FontName,bubble.FontSize,new RectangleF(-bubble.Width/2,top+i*lineHeight,bubble.Width,lineHeight),fill,bubble.Letters,true);
            }
            g.Restore(state);
        }
        public static PointF ToBubblePoint(Bubble b,PointF p){double a=-b.Rotation*Math.PI/180;float x=p.X-b.X,y=p.Y-b.Y;return new PointF((float)(x*Math.Cos(a)-y*Math.Sin(a)),(float)(x*Math.Sin(a)+y*Math.Cos(a)));}
        public Bubble Find(string id){return Bubbles.Find(delegate(Bubble b){return b.Id==id;});}
    }
}
