using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;
class Uninstall {
    [STAThread]static int Main(string[] args){Application.EnableVisualStyles();
        if(args.Length>=2&&args[0]=="--cleanup"){
            try{bool test=Array.IndexOf(args,"--test")>=0;int parent;if(args.Length>=3&&Int32.TryParse(args[2],out parent)&&parent>0)try{Process.GetProcessById(parent).WaitForExit(15000);}catch(ArgumentException){}Remove(args[1],test);if(!test)MessageBox.Show("콩으니 카드뉴스 생성 에디터를 제거했어요.","제거 완료");return 0;}catch(Exception e){if(Array.IndexOf(args,"--test")<0)MessageBox.Show(e.Message,"제거 오류");return 1;}
        }
        string root=AppDomain.CurrentDomain.BaseDirectory;if(MessageBox.Show("콩으니 카드뉴스 생성 에디터를 제거할까요?\n직접 저장한 PNG 파일은 삭제하지 않습니다.","프로그램 제거",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return 0;
        string temp=Path.Combine(Path.GetTempPath(),"Kongeuni-Uninstall-"+Guid.NewGuid().ToString("N")+".exe");File.Copy(Assembly.GetExecutingAssembly().Location,temp);
        Process.Start(new ProcessStartInfo(temp,"--cleanup \""+root.TrimEnd('\\')+"\" "+Process.GetCurrentProcess().Id){WindowStyle=ProcessWindowStyle.Hidden});return 0;
    }
    static void Remove(string root,bool test){root=Path.GetFullPath(root);string prefix=root.TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
        if(!File.Exists(Path.Combine(root,".kongeuni-install"))||File.ReadAllText(Path.Combine(root,".kongeuni-install"))!="KongeuniCardEditor:1.0")throw new InvalidDataException("설치 정보가 없어 이 폴더는 지우지 않습니다.");
        foreach(Process p in Process.GetProcessesByName("KongeuniCardEditor"))try{if(p.MainModule.FileName.StartsWith(prefix,StringComparison.OrdinalIgnoreCase))throw new IOException("실행 중인 에디터를 닫고 다시 제거해 주세요.");}catch(System.ComponentModel.Win32Exception){}
        string[] owned={"KongeuniCardEditor.exe","KongeuniCardEditor.exe.config","Uninstall.exe","사용법.txt","THIRD_PARTY_NOTICES.md","fonts/Galmuri11.ttf","fonts/Galmuri11-Bold.ttf","licenses/Galmuri-OFL.md",".kongeuni-install","owned-files.txt"};
        foreach(string item in owned){string file=Path.GetFullPath(Path.Combine(root,item));if(!file.StartsWith(prefix,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("경로 오류");if(File.Exists(file))File.Delete(file);}
        foreach(string folder in new string[]{"fonts","licenses"}){string path=Path.Combine(root,folder);if(Directory.Exists(path)&&Directory.GetFileSystemEntries(path).Length==0)Directory.Delete(path);}
        if(!test){using(RegistryKey key=Registry.CurrentUser.OpenSubKey("Software/Microsoft/Windows/CurrentVersion/Uninstall",true))if(key!=null)key.DeleteSubKeyTree("KongeuniCardEditor",false);string programs=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs),"콩으니 카드뉴스 에디터");foreach(string name in new string[]{"콩으니 카드뉴스 생성 에디터.lnk","프로그램 제거.lnk"}){string file=Path.Combine(programs,name);if(File.Exists(file))File.Delete(file);}if(Directory.Exists(programs)&&Directory.GetFileSystemEntries(programs).Length==0)Directory.Delete(programs);string desktop=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),"콩으니 카드뉴스 생성 에디터.lnk");if(File.Exists(desktop))File.Delete(desktop);}
        if(Directory.GetFileSystemEntries(root).Length==0)Directory.Delete(root);
    }
}
