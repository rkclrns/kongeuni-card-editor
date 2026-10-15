using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Windows.Forms;

namespace KongeuniEditor
{
    public sealed class PaintSpec
    {
        public Color First, Second;
        public bool Gradient;
        public int Angle = 90;
        public PaintSpec(Color a, Color b){First=a;Second=b;}
        public PaintSpec Copy(){return (PaintSpec)MemberwiseClone();}
        public bool Same(PaintSpec p){return First==p.First&&Second==p.Second&&Gradient==p.Gradient&&Angle==p.Angle;}
        public Brush Brush(RectangleF r){return Gradient ? (Brush)new LinearGradientBrush(new RectangleF(r.X,r.Y,Math.Max(1,r.Width),Math.Max(1,r.Height)),First,Second,Angle) : new SolidBrush(First);}
    }
    public sealed class TextLook
    {
        public bool Bold,Italic,Outline=true,Shadow=true,Glow;
        public Color OutlineColor=Color.White,GlowColor=Color.FromArgb(255,145,193);
        public int Stroke=18,ShadowDistance=9,ShadowOpacity=90,GlowRadius=16,GlowOpacity=100;
        public TextLook Copy(){return (TextLook)MemberwiseClone();}
        public bool Same(TextLook b){foreach(System.Reflection.FieldInfo f in typeof(TextLook).GetFields())if(!Object.Equals(f.GetValue(this),f.GetValue(b)))return false;return true;}
    }
    public sealed class Bubble
    {
        public string Id=Guid.NewGuid().ToString("N"), Text="이 테마 너무 귀여워!";
        public string FontName=Card.FixedFont;
        public float X=540,Y=780,Width=390,Height=120;
        public int Kind,FontSize=34,Border=2,Rotation;
        public Color Fill=Color.White,Line=Color.FromArgb(45,45,45),Ink=Color.FromArgb(35,35,35);
        public bool Shadow;
        public bool Mirrored;
        public TextLook Letters=new TextLook{Outline=false,Shadow=false,Stroke=3,ShadowDistance=4};
        public Bubble Copy(){Bubble copy=(Bubble)MemberwiseClone();copy.Letters=Letters.Copy();return copy;}
        public bool Same(Bubble b){foreach(System.Reflection.FieldInfo f in typeof(Bubble).GetFields())if(f.Name!="Letters"&&!Object.Equals(f.GetValue(this),f.GetValue(b)))return false;return Letters.Same(b.Letters);}
    }
    public sealed partial class Card : IDisposable
    {
        public const string FixedFont="Galmuri11 Regular";
        public const float TitleSize=1;
        public Bitmap Photo;
        public bool IsDemo=true;
        public string Name="포근한 민트",Account="",CardFontName="Galmuri11 Regular";
        public Color Background=Color.FromArgb(218,239,224),Accent=Color.FromArgb(50,110,83),Ink=Color.FromArgb(30,53,42);
        public PaintSpec BackgroundFill=new PaintSpec(Color.White,Color.White),PatternFill=new PaintSpec(Color.Gray,Color.Gray),TitleFill=new PaintSpec(Color.Black,Color.Gray);
        public Color FrameColor=Color.White;
        public TextLook TitleStyle=new TextLook{Bold=true},BrandStyle=new TextLook{Stroke=9,ShadowDistance=5,ShadowOpacity=95};
        public Color TitleOutline{get{return TitleStyle.OutlineColor;}set{TitleStyle.OutlineColor=value;}}
        public PaintSpec BrandFill=new PaintSpec(Color.FromArgb(91,119,103),Color.FromArgb(150,125,165));
        public Color BrandOutline{get{return BrandStyle.OutlineColor;}set{BrandStyle.OutlineColor=value;}}
        public int BrandStroke{get{return BrandStyle.Stroke;}set{BrandStyle.Stroke=value;}}
        public int BrandShadowDistance{get{return BrandStyle.ShadowDistance;}set{BrandStyle.ShadowDistance=value;}}
        public int BrandShadowOpacity{get{return BrandStyle.ShadowOpacity;}set{BrandStyle.ShadowOpacity=value;}}
        public bool BrandShadow{get{return BrandStyle.Shadow;}set{BrandStyle.Shadow=value;}}
        public int DotRowSpacing=140;
        public bool TitleAtBottom;
        public bool SupportsAndroid;
        public string PlatformLabel{get{return SupportsAndroid?"IOS/AND":"IOS";}}
        public int Width=1080,Height=1350,Pattern=1,PatternSize=48,DotSize=9,PatternOpacity=35;
        public int TitleStroke{get{return TitleStyle.Stroke;}set{TitleStyle.Stroke=value;}}
        public int TitleShadowDistance{get{return TitleStyle.ShadowDistance;}set{TitleStyle.ShadowDistance=value;}}
        public int TitleShadowOpacity{get{return TitleStyle.ShadowOpacity;}set{TitleStyle.ShadowOpacity=value;}}
        public bool TitleShadow{get{return TitleStyle.Shadow;}set{TitleStyle.Shadow=value;}}
        public int FrameWidth=22;
        public bool Frame=true,PhotoShadow=true;
        public float PhotoScale=1,PhotoX,PhotoY,TitleX,TitleY;
        public float BrandScale=1,BrandX,BrandY;
        public List<Color> Palette=new List<Color>();
        public List<Bubble> Bubbles=new List<Bubble>();
        public RectangleF PhotoBounds,TitleBounds,TitleGlyphBounds,BrandBounds,PlatformBounds;
        public Card(){
            Photo=new Bitmap(600,600);using(Graphics g=Graphics.FromImage(Photo))g.Clear(Color.FromArgb(205,205,205));
            ExtractPalette();Name="New Name";TitleAtBottom=true;PhotoY=30;
            BackgroundFill=new PaintSpec(Color.FromArgb(255,239,240),Color.FromArgb(255,239,240));PatternFill=new PaintSpec(Color.White,Color.White);Pattern=3;PatternSize=55;DotSize=18;DotRowSpacing=100;PatternOpacity=100;
            TitleFill=new PaintSpec(Color.FromArgb(80,80,80),Color.FromArgb(80,80,80));BrandFill=new PaintSpec(Color.FromArgb(65,65,65),Color.FromArgb(65,65,65));
        }
        public void Dispose(){if(Photo!=null)Photo.Dispose();}
        public void Load(string path)
        {
            // Decode and detach before replacing the previous photo; source files stay unlocked.
            Bitmap next;
            using (Image raw = Image.FromFile(path))
            {
                if ((long)raw.Width * raw.Height > 60000000) throw new ArgumentException("사진이 너무 커요. 6천만 픽셀 이하의 이미지를 사용해 주세요.");
                try {
                    if (Array.IndexOf(raw.PropertyIdList, 0x112) >= 0) {
                        int o = raw.GetPropertyItem(0x112).Value[0];
                        RotateFlipType[] flips = { RotateFlipType.RotateNoneFlipNone, RotateFlipType.RotateNoneFlipNone, RotateFlipType.RotateNoneFlipX, RotateFlipType.Rotate180FlipNone, RotateFlipType.Rotate180FlipX, RotateFlipType.Rotate90FlipX, RotateFlipType.Rotate90FlipNone, RotateFlipType.Rotate270FlipX, RotateFlipType.Rotate270FlipNone };
                        if (o >= 1 && o <= 8) raw.RotateFlip(flips[o]);
                    }
                } catch (ArgumentException) { }
                next = new Bitmap(raw.Width, raw.Height, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(next)) g.DrawImage(raw, 0, 0, raw.Width, raw.Height);
            }
            Photo = next; IsDemo = false;
            PhotoScale = 1; PhotoX = PhotoY = 0;
            ExtractPalette(); Name = SuggestName(Accent);
        }
        public Card Snapshot(){Card c=(Card)MemberwiseClone();c.Palette=new List<Color>(Palette);c.BackgroundFill=BackgroundFill.Copy();c.PatternFill=PatternFill.Copy();c.TitleFill=TitleFill.Copy();c.BrandFill=BrandFill.Copy();c.TitleStyle=TitleStyle.Copy();c.BrandStyle=BrandStyle.Copy();c.Bubbles=new List<Bubble>();foreach(Bubble b in Bubbles)c.Bubbles.Add(b.Copy());return c;}
        public void Restore(Card s){foreach(System.Reflection.FieldInfo f in typeof(Card).GetFields())if(!f.IsLiteral)f.SetValue(this,f.GetValue(s));Card copy=s.Snapshot();Palette=copy.Palette;BackgroundFill=copy.BackgroundFill;PatternFill=copy.PatternFill;TitleFill=copy.TitleFill;BrandFill=copy.BrandFill;TitleStyle=copy.TitleStyle;BrandStyle=copy.BrandStyle;Bubbles=copy.Bubbles;}
        public bool SameAs(Card s){
            foreach(System.Reflection.FieldInfo f in typeof(Card).GetFields()){
                if(f.IsLiteral||f.Name=="Palette"||f.Name=="Bubbles"||f.Name=="PhotoBounds"||f.Name=="TitleBounds"||f.Name=="TitleGlyphBounds"||f.Name=="BrandBounds"||f.Name=="PlatformBounds"||f.FieldType==typeof(PaintSpec)||f.FieldType==typeof(TextLook))continue;
                if(!Object.Equals(f.GetValue(this),f.GetValue(s)))return false;
            }
            if(!TitleStyle.Same(s.TitleStyle)||!BrandStyle.Same(s.BrandStyle)||!BackgroundFill.Same(s.BackgroundFill)||!PatternFill.Same(s.PatternFill)||!TitleFill.Same(s.TitleFill)||!BrandFill.Same(s.BrandFill)||Palette.Count!=s.Palette.Count||Bubbles.Count!=s.Bubbles.Count)return false;
            for(int i=0;i<Palette.Count;i++)if(Palette[i]!=s.Palette[i])return false;
            for(int i=0;i<Bubbles.Count;i++)if(!Bubbles[i].Same(s.Bubbles[i]))return false;
            return true;
        }
        public void ExtractPalette()
        {
            Dictionary<int, long[]> bins = new Dictionary<int, long[]>();
            // Sample the screenshot's interior so black status bars and white UI do not dominate.
            int step = Math.Max(1, Math.Max(Photo.Width, Photo.Height) / 160);
            for (int y = Photo.Height / 12; y < Photo.Height * 11 / 12; y += step)
                for (int x = Photo.Width / 20; x < Photo.Width * 19 / 20; x += step)
                {
                    Color c = Photo.GetPixel(x, y);
                    if (c.A < 180 || c.GetBrightness() < .14 || c.GetBrightness() > .96 || c.GetSaturation() < .07) continue;
                    int key = (c.R / 24 << 8) | (c.G / 24 << 4) | c.B / 24;
                    if (!bins.ContainsKey(key)) bins[key] = new long[4];
                    long[] b = bins[key]; b[0]++; b[1] += c.R; b[2] += c.G; b[3] += c.B;
                }
            List<long[]> ranked = new List<long[]>(bins.Values);
            ranked.Sort(delegate(long[] a, long[] b) { return b[0].CompareTo(a[0]); });
            Palette.Clear();
            foreach (long[] b in ranked)
            {
                Color c = Color.FromArgb((int)(b[1]/b[0]), (int)(b[2]/b[0]), (int)(b[3]/b[0]));
                bool near = false;
                foreach (Color p in Palette) if (Distance(c, p) < 55) near = true;
                if (!near) Palette.Add(c);
                if (Palette.Count == 5) break;
            }
            if (Palette.Count == 0) Palette.Add(Color.FromArgb(131, 144, 153));
            SetPalette(Palette[0]);
        }
        public void SetPalette(Color c)
        {
            Background = Mix(c, Color.White, .77f);
            Accent = c.GetBrightness() > .52 ? Mix(c, Color.Black, .35f) : c;
            Ink = Mix(Accent, Color.Black, .52f);
            BackgroundFill = new PaintSpec(Background, Mix(Background,Color.White,.7f));
            PatternFill = new PaintSpec(Accent, Mix(Accent,Color.White,.4f));
            TitleFill = new PaintSpec(Accent, Mix(Accent,Color.White,.3f));
        }
        static double Distance(Color a, Color b) { return Math.Sqrt(Math.Pow(a.R-b.R,2)+Math.Pow(a.G-b.G,2)+Math.Pow(a.B-b.B,2)); }
        public static Color Mix(Color a, Color b, float t) { return Color.FromArgb((int)(a.R*(1-t)+b.R*t), (int)(a.G*(1-t)+b.G*t), (int)(a.B*(1-t)+b.B*t)); }
        public static string SuggestName(Color c)
        {
            if (c.GetSaturation() < .13) return "차분한 모노";
            float h = c.GetHue();
            if (h < 18 || h >= 345) return "말랑 로즈";
            if (h < 45) return "달콤한 피치";
            if (h < 72) return "버터 옐로";
            if (h < 165) return "포근한 민트";
            if (h < 205) return "맑은 소다";
            if (h < 255) return "보송 블루";
            if (h < 290) return "라일락 한 스푼";
            return "몽글 핑크";
        }
        public Bitmap Render(){Bitmap b=new Bitmap(Width,Height,PixelFormat.Format32bppArgb);b.SetResolution(144,144);using(Graphics g=Graphics.FromImage(b))Draw(g);return b;}
        public static void Text(Graphics g,string text,float size,Color color,RectangleF area,bool bold,bool center){TextWithFont(g,text,size,color,area,bold,center,"맑은 고딕");}
        public static Font MakeFont(string family,float size,FontStyle desired,GraphicsUnit unit)
        {
            try {
                using(FontFamily f=AppFonts.Family(family)) {
                    FontStyle style=f.IsStyleAvailable(desired)?desired:f.IsStyleAvailable(FontStyle.Regular)?FontStyle.Regular:f.IsStyleAvailable(FontStyle.Bold)?FontStyle.Bold:f.IsStyleAvailable(FontStyle.Italic)?FontStyle.Italic:FontStyle.Bold|FontStyle.Italic;
                    return new Font(f,size,style,unit);
                }
            } catch(ArgumentException) { return new Font("맑은 고딕",size,desired,unit); }
        }
        static void TextWithFont(Graphics g,string text,float size,Color color,RectangleF area,bool bold,bool center,string family)
        {
            using(StringFormat fmt=new StringFormat()) {
                fmt.Alignment=center?StringAlignment.Center:StringAlignment.Near;fmt.LineAlignment=StringAlignment.Center;
                fmt.FormatFlags=StringFormatFlags.NoWrap;fmt.Trimming=StringTrimming.EllipsisCharacter;
                float actual=size;
                while(actual>16) {
                    using(Font f=MakeFont(family,actual,bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel))
                        if(g.MeasureString(text,f).Width<=area.Width && g.MeasureString(text,f).Height<=area.Height) break;
                    actual-=2;
                }
                using(Font f=MakeFont(family,actual,bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel))
                using(Brush b=new SolidBrush(color)) g.DrawString(text,f,b,area,fmt);
            }
        }
        public static Bitmap Demo()
        {
            Bitmap b=new Bitmap(600,1050);
            using(Graphics g=Graphics.FromImage(b)) {
                g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Color.FromArgb(191,219,200));
                using(Brush top=new SolidBrush(Color.FromArgb(218,237,223))) g.FillRectangle(top,0,0,600,145);
                Text(g,"9:41",22,Color.FromArgb(47,75,56),new RectangleF(30,10,120,38),true,false);
                Text(g,"‹     민트와의 하루",30,Color.FromArgb(47,75,56),new RectangleF(30,65,530,58),true,false);
                Text(g,"10월 16일 금요일",19,Color.FromArgb(76,107,84),new RectangleF(100,180,400,40),false,true);
                using(Brush avatar=new SolidBrush(Color.FromArgb(104,150,113))) g.FillEllipse(avatar,29,265,66,66);
                using(Brush white=new SolidBrush(Color.FromArgb(250,252,242))) {
                    Round(g,white,new RectangleF(112,270,338,97),23);
                    Round(g,white,new RectangleF(112,382,300,97),23);
                    Round(g,white,new RectangleF(112,665,332,97),23);
                }
                using(Brush green=new SolidBrush(Color.FromArgb(79,128,91))) {
                    Round(g,green,new RectangleF(217,520,352,97),23);
                    Round(g,green,new RectangleF(270,805,299,97),23);
                }
                Color ink=Color.FromArgb(44,68,51);
                Text(g,"오늘도 포근한 하루",25,ink,new RectangleF(137,285,290,65),false,false);
                Text(g,"이 초록색 너무 좋다",25,ink,new RectangleF(137,398,258,65),false,false);
                Text(g,"너한테도 공유해 줄게",25,Color.White,new RectangleF(242,537,305,65),false,false);
                Text(g,"우리 취향이잖아 :)",25,ink,new RectangleF(137,681,285,65),false,false);
                Text(g,"마음에 쏙 들어!",25,Color.White,new RectangleF(295,822,250,65),false,false);
                using(Brush white=new SolidBrush(Color.FromArgb(248,250,246))) g.FillRectangle(white,0,967,600,83);
                Text(g,"+     메시지 입력                                     ◯",23,Color.Gray,new RectangleF(25,983,555,52),false,false);
            }
            return b;
        }
    }
}
