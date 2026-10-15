using System;
using System.IO;
using System.Windows.Forms;
namespace KongeuniEditor
{
    static class Program
    {
        public static bool IsTesting;
        [STAThread] static int Main(string[] args){
            AppFonts.Load();Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
            if(args.Length==2&&args[0]=="--self-test"){
                IsTesting=true;try{Tests.Run(args[1]);return 0;}catch(Exception e){File.WriteAllText(Path.Combine(args[1],"ERROR.txt"),e.ToString());return 1;}
            }
            Application.Run(new Studio());return 0;
        }
    }
}
