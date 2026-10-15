using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Windows.Forms;
namespace KongeuniEditor
{
    static class Tests
    {
        static void Check(bool ok,string reason){if(!ok)throw new Exception(reason);}
        static T Find<T>(Control root,Predicate<T> test) where T:Control{foreach(Control c in root.Controls){T found=c as T;if(found!=null&&test(found))return found;T nested=Find<T>(c,test);if(nested!=null)return nested;}return null;}
        static void Shot(Form f,string path){if(!f.Visible){f.Opacity=0;f.ShowInTaskbar=false;f.Show();}Application.DoEvents();using(Bitmap image=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(image,new Rectangle(0,0,image.Width,image.Height));image.Save(path,ImageFormat.Png);}}
        static void Mouse(Preview p,string name,Point point){typeof(Preview).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(p,new object[]{new MouseEventArgs(MouseButtons.Left,1,point.X,point.Y,0)});Application.DoEvents();}
        public static void Run(string folder){
            Directory.CreateDirectory(folder);
            using(Card c=new Card())using(EditHistory history=new EditHistory(c)){
                Check(c.Name=="New Name","startup name wrong");Check(c.Photo.Width==c.Photo.Height&&c.Photo.Width==600,"placeholder not square");Check(c.Bubbles.Count==0,"old sample callout remains");
                for(int y=0;y<c.Photo.Height;y+=17)for(int x=0;x<c.Photo.Width;x+=17)Check(c.Photo.GetPixel(x,y).ToArgb()==Color.FromArgb(205,205,205).ToArgb(),"placeholder not uniform gray");
                Check(!c.SupportsAndroid&&c.PlatformLabel=="IOS","platform default wrong");
                foreach(int height in new int[]{1080,1350,1920}){
                    c.Height=height;using(Bitmap image=c.Render()){Check(image.Width==1080&&image.Height==height,"PNG dimensions incorrect");Check(c.PlatformBounds.Right<1080&&c.PlatformBounds.Left>700,"IOS not anchored to top right");Check(c.PlatformBounds.Top>20&&c.PlatformBounds.Bottom<130,"platform vertical location incorrect");image.Save(Path.Combine(folder,"IOS-"+height+".png"),ImageFormat.Png);}
                    float right=c.PlatformBounds.Right,width=c.PlatformBounds.Width;history.Change(delegate{c.SupportsAndroid=true;},null);
                    using(Bitmap image=c.Render()){Check(c.PlatformLabel=="IOS/AND","AND label wrong");Check(Math.Abs(c.PlatformBounds.Right-right)<.01&&c.PlatformBounds.Width>width,"combined label not right aligned");image.Save(Path.Combine(folder,"IOS-AND-"+height+".png"),ImageFormat.Png);}
                    history.Undo();Check(c.PlatformLabel=="IOS","platform undo failed");history.Redo();Check(c.PlatformLabel=="IOS/AND","platform redo failed");history.Change(delegate{c.SupportsAndroid=false;},null);
                }
                c.Height=1350;c.SupportsAndroid=true;history.Change(delegate{c.BrandStyle.Glow=true;c.BrandStyle.Italic=true;c.BrandFill.First=Color.FromArgb(155,100,155);},null);using(Bitmap image=c.Render())image.Save(Path.Combine(folder,"platform-styled.png"),ImageFormat.Png);history.Undo();Check(!c.BrandStyle.Glow,"shared styles undo failed");
                string source=Path.Combine(folder,"test-image.png");using(Bitmap image=new Bitmap(320,640)){using(Graphics g=Graphics.FromImage(image))g.Clear(Color.PeachPuff);image.Save(source,ImageFormat.Png);}history.Change(delegate{c.Load(source);},null);Check(c.Photo.Width==320&&!c.IsDemo&&c.SupportsAndroid,"photo replacement altered platform");history.Undo();Check(c.Name=="New Name"&&c.Photo.Width==600&&c.IsDemo,"import undo did not restore fresh state");
            }
            using(Studio studio=new Studio()){
                Shot(studio,Path.Combine(folder,"startup-editor.png"));Check(studio.Model.Name=="New Name"&&studio.Model.Bubbles.Count==0,"editor startup not clean");
                TabControl tabs=Find<TabControl>(studio,delegate(TabControl t){return true;});Preview p=studio.PreviewCanvas;RectangleF label=studio.Model.PlatformBounds,display=p.Display;Point point=new Point((int)(display.X+(label.Left+label.Width/2)*p.Zoom),(int)(display.Y+(label.Top+label.Height/2)*p.Zoom));Mouse(p,"OnMouseDown",point);Mouse(p,"OnMouseUp",point);Check(tabs.SelectedIndex==3,"platform click did not open signature settings");
                CheckBox android=Find<CheckBox>(studio,delegate(CheckBox c){return c.Text.StartsWith("AND도 지원");});Check(android!=null&&!android.Checked,"AND checkbox absent or on by default");android.Checked=true;Application.DoEvents();Check(studio.Model.SupportsAndroid&&studio.Model.PlatformLabel=="IOS/AND","checkbox not wired");Shot(studio,Path.Combine(folder,"and-editor.png"));
                studio.History.Undo();Check(!android.Checked&&studio.Model.PlatformLabel=="IOS","checkbox undo failed");studio.History.Redo();Check(android.Checked&&studio.Model.PlatformLabel=="IOS/AND","checkbox redo failed");
                using(Bitmap image=studio.Model.Render())image.Save(Path.Combine(folder,"default-IOS-AND.png"),ImageFormat.Png);studio.Close();
            }
            File.WriteAllText(Path.Combine(folder,"PASS.txt"),"PASS: New Name startup; 600x600 uniform gray placeholder; no old screenshot/callout; IOS default; IOS/AND checkbox; top-right anchor in 3 PNG sizes; shared branding style; undo/redo and UI synchronization; platform click opens inspector; image import preserves platform and undo restores placeholder; UI and exported PNG render.");
        }
    }
}
