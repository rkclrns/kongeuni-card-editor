using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;
namespace KongeuniEditor {
    static class AppFonts {
        static readonly PrivateFontCollection fonts=new PrivateFontCollection();
        static readonly List<string> files=new List<string>();
        [DllImport("gdi32.dll",CharSet=CharSet.Unicode)]static extern int AddFontResourceEx(string path,uint flags,IntPtr reserved);
        public static void Load(){string folder=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"fonts");if(!Directory.Exists(folder))return;foreach(string file in Directory.GetFiles(folder,"*.ttf")){fonts.AddFontFile(file);AddFontResourceEx(file,0x10,IntPtr.Zero);files.Add(file);}}
        public static string[] Names {get {List<string> names=new List<string>();foreach(FontFamily family in fonts.Families)names.Add(family.Name);return names.ToArray();}}
        public static FontFamily Family(string name){foreach(FontFamily family in fonts.Families)if(family.Name==name)return family;return new FontFamily(name);}
    }
}
