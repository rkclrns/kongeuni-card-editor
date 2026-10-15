using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;
class Setup {
    public const string Marker="KongeuniCardEditor:1.0";
    [STAThread]static int Main(string[] args){Application.EnableVisualStyles();if(args.Length==2&&args[0]=="--test-install"){try{Install(args[1],true,false);return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}Application.Run(new SetupForm());return 0;}
    public static void Install(string root,bool test,bool desktop){
        root=Path.GetFullPath(root);Directory.CreateDirectory(root);string prefix=root.TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
        foreach(Process p in Process.GetProcessesByName("KongeuniCardEditor"))try{if(p.MainModule.FileName.StartsWith(prefix,StringComparison.OrdinalIgnoreCase))throw new IOException("에디터를 먼저 닫은 뒤 다시 설치해 주세요.");}catch(System.ComponentModel.Win32Exception){}
        string[] allowed={"KongeuniCardEditor.exe","KongeuniCardEditor.exe.config","Uninstall.exe","사용법.txt","THIRD_PARTY_NOTICES.md","fonts/Galmuri11.ttf","fonts/Galmuri11-Bold.ttf","licenses/Galmuri-OFL.md"};
        using(Stream payload=Assembly.GetExecutingAssembly().GetManifestResourceStream("payload"))using(ZipArchive zip=new ZipArchive(payload,ZipArchiveMode.Read)){
            foreach(ZipArchiveEntry entry in zip.Entries){if(entry.Name.Length==0)continue;string relative=entry.FullName.Replace('\\','/');if(Array.IndexOf(allowed,relative)<0)throw new InvalidDataException("알 수 없는 설치 파일");string destination=Path.GetFullPath(Path.Combine(root,relative));if(!destination.StartsWith(prefix,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("설치 경로가 올바르지 않습니다.");Directory.CreateDirectory(Path.GetDirectoryName(destination));entry.ExtractToFile(destination,true);}
        }
        File.WriteAllText(Path.Combine(root,".kongeuni-install"),Marker);File.WriteAllLines(Path.Combine(root,"owned-files.txt"),allowed);
        if(test)return;
        string app=Path.Combine(root,"KongeuniCardEditor.exe"),uninstall=Path.Combine(root,"Uninstall.exe"),programs=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs),"콩으니 카드뉴스 에디터");Directory.CreateDirectory(programs);
        Shortcut(Path.Combine(programs,"콩으니 카드뉴스 생성 에디터.lnk"),app,root);Shortcut(Path.Combine(programs,"프로그램 제거.lnk"),uninstall,root);
        if(desktop)Shortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),"콩으니 카드뉴스 생성 에디터.lnk"),app,root);
        using(RegistryKey key=Registry.CurrentUser.CreateSubKey("Software/Microsoft/Windows/CurrentVersion/Uninstall/KongeuniCardEditor")){key.SetValue("DisplayName","콩으니 카드뉴스 생성 에디터");key.SetValue("DisplayVersion","1.0.0");key.SetValue("Publisher","콩으니");key.SetValue("InstallLocation",root);key.SetValue("DisplayIcon",app);key.SetValue("UninstallString","\""+uninstall+"\"");key.SetValue("NoModify",1);key.SetValue("NoRepair",1);}
    }
    static void Shortcut(string path,string target,string working){Type type=Type.GetTypeFromProgID("WScript.Shell");object shell=Activator.CreateInstance(type),shortcut=type.InvokeMember("CreateShortcut",BindingFlags.InvokeMethod,null,shell,new object[]{path});try{Type item=shortcut.GetType();item.InvokeMember("TargetPath",BindingFlags.SetProperty,null,shortcut,new object[]{target});item.InvokeMember("WorkingDirectory",BindingFlags.SetProperty,null,shortcut,new object[]{working});item.InvokeMember("IconLocation",BindingFlags.SetProperty,null,shortcut,new object[]{target+",0"});item.InvokeMember("Save",BindingFlags.InvokeMethod,null,shortcut,null);}finally{Marshal.FinalReleaseComObject(shortcut);Marshal.FinalReleaseComObject(shell);}}
}
class SetupForm:Form {
    readonly Label message;readonly CheckBox desktop,run;readonly Button install;
    public SetupForm(){Text="콩으니 카드뉴스 에디터 설치 · 1.0.0";ClientSize=new Size(570,340);Font=new Font("맑은 고딕",10);BackColor=Color.FromArgb(250,250,252);StartPosition=FormStartPosition.CenterScreen;FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;
        using(Stream stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("appicon"))using(Icon icon=new Icon(stream))Icon=(Icon)icon.Clone();
        Controls.Add(new Label{Text="콩으니 카드뉴스 생성 에디터",Font=new Font("맑은 고딕",19,FontStyle.Bold),AutoSize=true,Location=new Point(26,27)});
        Controls.Add(new Label{Text="Windows용 · 버전 1.0.0 · 관리자 권한 없이 내 계정에 설치",AutoSize=true,Location=new Point(28,82)});
        string root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Programs","KongeuniCardEditor");
        Controls.Add(new Label{Text="설치 위치\n"+root,Location=new Point(28,123),Size=new Size(515,55)});
        desktop=new CheckBox{Text="바탕화면 바로가기 만들기",Checked=true,AutoSize=true,Location=new Point(28,192)};run=new CheckBox{Text="설치 후 프로그램 실행",Checked=true,AutoSize=true,Location=new Point(28,222)};Controls.Add(desktop);Controls.Add(run);
        message=new Label{Text="갈무리11 서체 포함 · 사진은 컴퓨터 안에서만 처리해요.",Location=new Point(28,267),Size=new Size(330,52),ForeColor=Color.DimGray};Controls.Add(message);
        install=new Button{Text="설치하기",Location=new Point(398,272),Size=new Size(140,43),FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(50,50,57),ForeColor=Color.White};Controls.Add(install);install.Click+=delegate{try{install.Enabled=false;message.Text="설치 중…";Application.DoEvents();Setup.Install(root,false,desktop.Checked);message.Text="설치가 완료됐어요.";if(run.Checked)Process.Start(Path.Combine(root,"KongeuniCardEditor.exe"));Close();}catch(Exception e){message.Text="설치를 완료하지 못했어요.";MessageBox.Show(this,e.Message,"설치 오류");install.Enabled=true;}};
    }
}
