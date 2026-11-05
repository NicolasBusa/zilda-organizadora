using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace ZildaSetup {
    public static class Setup {
        static string[] args;
        static string destination;
        static bool test;
        const string Marker="zilda-install.txt";
        static string[] Files={"ZildaOrganizadora.exe","ZildaOrganizadora.exe.config","DesinstalarZilda.exe","LEIA-ME.txt","LICENCA.txt",Marker};
        static string Value(string name,string fallback){int i=Array.IndexOf(args,name);return i>=0&&i+1<args.Length?args[i+1]:fallback;}
        static string Quote(string s){return "\""+s+"\"";}
        [STAThread] public static int Main(string[] command){
            args=command;test=args.Contains("--test");destination=Path.GetFullPath(Value("--destination",Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Programs","ZildaOrganizadora")));
            try {
                if(args.Contains("--cleanup")){Cleanup();return 0;}
                bool uninstall=args.Contains("--uninstall")||Path.GetFileName(Assembly.GetExecutingAssembly().Location).Equals("DesinstalarZilda.exe",StringComparison.OrdinalIgnoreCase);
                if(uninstall&&!args.Contains("--destination"))destination=Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                ValidateTarget();
                if(!Environment.Is64BitOperatingSystem)throw new InvalidOperationException("A Zilda precisa de Windows de 64 bits.");
                using(var key=Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full")){if(key==null||Convert.ToInt32(key.GetValue("Release",0))<528040)throw new InvalidOperationException("Este computador precisa do .NET Framework 4.8. Instale-o pelo Windows Update e execute novamente.");}
                if(args.Contains("--silent")){if(uninstall)Uninstall(false);else Install(false,false);return 0;}
                Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.Run(new SetupForm(uninstall));return 0;
            }catch(Exception ex){try{string log=Path.Combine(Path.GetTempPath(),"Zilda-Setup-erro.txt");File.WriteAllText(log,DateTime.Now.ToString("s")+"\n"+ex);}catch{}if(!args.Contains("--silent"))MessageBox.Show(ex.Message,"Zilda Organizadora",MessageBoxButtons.OK,MessageBoxIcon.Error);return 1;}
        }
        static void ValidateTarget(){
            string target=destination.TrimEnd(Path.DirectorySeparatorChar);
            if(String.Equals(target,Path.GetPathRoot(target),StringComparison.OrdinalIgnoreCase)||target.Length<12)throw new InvalidOperationException("Pasta de instalação inválida.");
            foreach(string forbidden in new[]{Environment.GetFolderPath(Environment.SpecialFolder.Windows),Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)})if(String.Equals(target,forbidden.TrimEnd('\\'),StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Escolha uma pasta exclusiva para a Zilda.");
            if(Directory.Exists(destination)&&Directory.EnumerateFileSystemEntries(destination).Any()&&!File.Exists(Path.Combine(destination,Marker)))throw new InvalidOperationException("A pasta contém outros arquivos. Escolha uma pasta vazia e exclusiva para a Zilda.");
        }
        static void StopApp(bool restore){
            string exe=Path.Combine(destination,"ZildaOrganizadora.exe");if(!File.Exists(exe))return;
            string switches=restore?"--restore-archives --quit":"--quit";
            if(test)switches+=" --data-dir "+Quote(Path.Combine(destination,"test-data"));
            using(var p=Process.Start(new ProcessStartInfo(exe,switches){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden})){if(!p.WaitForExit(30000)||p.ExitCode!=0)throw new IOException("Feche a Zilda e tente novamente. A restauração dos atalhos não foi concluída.");}
        }
        public static void Install(bool startup,bool launch){
            ValidateTarget();if(File.Exists(Path.Combine(destination,Marker)))ValidateMarker();StopApp(false);Directory.CreateDirectory(destination);
            using(var resource=Assembly.GetExecutingAssembly().GetManifestResourceStream("zilda-payload.zip"))using(var zip=new ZipArchive(resource,ZipArchiveMode.Read)){
                foreach(var entry in zip.Entries){if(!Files.Contains(entry.FullName)||entry.FullName=="DesinstalarZilda.exe"||entry.FullName==Marker)throw new InvalidDataException("Pacote de instalação inválido.");string file=Path.Combine(destination,entry.FullName);string temp=file+".novo";if(File.Exists(temp))File.Delete(temp);entry.ExtractToFile(temp,false);if(File.Exists(file))File.Delete(file);File.Move(temp,file);}
            }
            File.Copy(Assembly.GetExecutingAssembly().Location,Path.Combine(destination,"DesinstalarZilda.exe"),true);
            File.WriteAllText(Path.Combine(destination,Marker),"ZILDA-ORGANIZADORA-INSTALL-v1\r\n"+destination+"\r\n");
            if(!test){
                string programs=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs),"Zilda Organizadora");Directory.CreateDirectory(programs);Shortcut(Path.Combine(programs,"Zilda Organizadora.lnk"),Path.Combine(destination,"ZildaOrganizadora.exe"),"");Shortcut(Path.Combine(programs,"Desinstalar Zilda.lnk"),Path.Combine(destination,"DesinstalarZilda.exe"),"--uninstall");
                using(var key=Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\ZildaOrganizadora")){key.SetValue("DisplayName","Zilda Organizadora");key.SetValue("DisplayVersion","1.0.0");key.SetValue("Publisher","Zilda Organizadora");key.SetValue("InstallLocation",destination);key.SetValue("DisplayIcon",Path.Combine(destination,"ZildaOrganizadora.exe"));key.SetValue("UninstallString",Quote(Path.Combine(destination,"DesinstalarZilda.exe"))+" --uninstall");key.SetValue("NoModify",1,RegistryValueKind.DWord);key.SetValue("NoRepair",1,RegistryValueKind.DWord);key.SetValue("EstimatedSize",(int)(Directory.GetFiles(destination).Sum(f=>new FileInfo(f).Length)/1024),RegistryValueKind.DWord);}
                using(var key=Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run")){if(startup)key.SetValue("ZildaOrganizadora",Quote(Path.Combine(destination,"ZildaOrganizadora.exe"))+" --tray");}
                if(launch)Process.Start(new ProcessStartInfo(Path.Combine(destination,"ZildaOrganizadora.exe")){UseShellExecute=true});
            }
        }
        static void Shortcut(string path,string target,string arguments){Type t=Type.GetTypeFromProgID("WScript.Shell");object shell=Activator.CreateInstance(t),link=null;try{link=t.InvokeMember("CreateShortcut",BindingFlags.InvokeMethod,null,shell,new object[]{path});link.GetType().InvokeMember("TargetPath",BindingFlags.SetProperty,null,link,new object[]{target});link.GetType().InvokeMember("Arguments",BindingFlags.SetProperty,null,link,new object[]{arguments});link.GetType().InvokeMember("WorkingDirectory",BindingFlags.SetProperty,null,link,new object[]{destination});link.GetType().InvokeMember("Save",BindingFlags.InvokeMethod,null,link,null);}finally{if(link!=null)Marshal.FinalReleaseComObject(link);Marshal.FinalReleaseComObject(shell);}}
        public static void Uninstall(bool removeData){
            ValidateTarget();ValidateMarker();
            StopApp(true);
            string dataRoot=test?Path.Combine(destination,"test-data"):Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"ZildaOrganizadora");
            string archive=Path.Combine(dataRoot,"arquivo");
            if(Directory.Exists(archive)&&Directory.EnumerateFileSystemEntries(archive).Any())throw new IOException("Há arquivos originais que ainda não puderam ser restaurados. A desinstalação foi interrompida e seus dados foram preservados. Abra a pasta de dados e recupere os arquivos antes de tentar novamente.");
            if(!test){
                using(var key=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run",true)){if(key!=null)key.DeleteValue("ZildaOrganizadora",false);}
                using(var key=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall",true)){if(key!=null)key.DeleteSubKeyTree("ZildaOrganizadora",false);}
                string programs=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs),"Zilda Organizadora");foreach(string file in new[]{"Zilda Organizadora.lnk","Desinstalar Zilda.lnk"}){string p=Path.Combine(programs,file);if(File.Exists(p))File.Delete(p);}if(Directory.Exists(programs)&&!Directory.EnumerateFileSystemEntries(programs).Any())Directory.Delete(programs);
                if(removeData){string data=Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"ZildaOrganizadora"));if(Directory.Exists(data)&&Path.GetFileName(data)=="ZildaOrganizadora")Directory.Delete(data,true);}
            }
            string own=Assembly.GetExecutingAssembly().Location;
            if(!String.Equals(Path.GetDirectoryName(own),destination,StringComparison.OrdinalIgnoreCase)){DeleteInstalled();return;}
            string helper=Path.Combine(Path.GetTempPath(),"Zilda-Uninstall-"+Guid.NewGuid().ToString("N")+".exe");File.Copy(own,helper,false);
            string command="--cleanup --destination "+Quote(destination)+" --parent "+Process.GetCurrentProcess().Id+(test?" --test":"");
            Process.Start(new ProcessStartInfo(helper,command){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden});
        }
        static void Cleanup(){int pid;int.TryParse(Value("--parent","0"),out pid);if(pid>0)try{using(var p=Process.GetProcessById(pid))if(!p.WaitForExit(30000))return;}catch(ArgumentException){}ValidateTarget();DeleteInstalled();}
        static void DeleteInstalled(){
            ValidateMarker();
            foreach(string name in Files){if(name==Marker)continue;string p=Path.Combine(destination,name);if(File.Exists(p))File.Delete(p);}
            File.Delete(Path.Combine(destination,Marker));if(!Directory.EnumerateFileSystemEntries(destination).Any())Directory.Delete(destination);
        }
        static void ValidateMarker(){
            string marker=Path.Combine(destination,Marker);if(!File.Exists(marker))throw new InvalidOperationException("Marcador de instalação ausente.");
            string[] lines=File.ReadAllLines(marker);if(lines.Length<2||lines[0]!="ZILDA-ORGANIZADORA-INSTALL-v1"||!String.Equals(Path.GetFullPath(lines[1]),destination,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Marcador de instalação inválido.");
        }
        class SetupForm:Form {
            CheckBox startup,launch,remove;Button action;Label status;bool uninstall;
            Color back=Color.FromArgb(16,19,28),ink=Color.FromArgb(235,237,246),muted=Color.FromArgb(154,163,184),accent=Color.FromArgb(160,135,255);
            public SetupForm(bool removing){
                uninstall=removing;Text=removing?"Desinstalar Zilda Organizadora":"Instalar Zilda Organizadora";ClientSize=new Size(600,490);BackColor=back;ForeColor=ink;Font=new Font("Segoe UI",10);FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;StartPosition=FormStartPosition.CenterScreen;AutoScaleMode=AutoScaleMode.Dpi;
                try{Icon=Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);}catch{}
                var logo=new Label{Text="Z",Font=new Font("Segoe UI",27,FontStyle.Bold),BackColor=accent,ForeColor=back,TextAlign=ContentAlignment.MiddleCenter,Bounds=new Rectangle(32,30,62,62)};Controls.Add(logo);
                AddLabel("Zilda Organizadora",24,FontStyle.Bold,ink,114,29,450,40);AddLabel("Um lugar para cada coisa.",11,FontStyle.Regular,muted,116,73,420,24);
                AddLabel(removing?"Remover deste computador":"Sua área de trabalho, mais organizada.",16,FontStyle.Bold,ink,32,124,540,34);
                AddLabel(removing?"Os atalhos arquivados serão devolvidos à área de trabalho antes da remoção. Seus perfis podem ser preservados para uma reinstalação.":"Grupos que abrem por hover ou clique. Cinco layouts, perfis e seus atalhos sempre por perto. Sem conta, sem assinatura, sem conexão.",11,FontStyle.Regular,muted,32,170,536,66);
                AddLabel((removing?"INSTALAÇÃO":"INSTALAR PARA ESTE USUÁRIO")+"\n"+destination,9,FontStyle.Regular,muted,32,246,536,48);
                if(removing){remove=new CheckBox{Text="Apagar também meus perfis e configurações",ForeColor=ink,Checked=false,Bounds=new Rectangle(32,310,540,26)};Controls.Add(remove);}else{startup=new CheckBox{Text="Iniciar a Zilda ao entrar no Windows",ForeColor=ink,Checked=true,Bounds=new Rectangle(32,305,540,26)};launch=new CheckBox{Text="Abrir a Zilda depois da instalação",ForeColor=ink,Checked=true,Bounds=new Rectangle(32,339,540,26)};Controls.Add(startup);Controls.Add(launch);}
                status=AddLabel("Windows 10/11 de 64 bits · .NET Framework 4.8 · Sem administrador",9,FontStyle.Regular,muted,32,390,536,25);
                action=new Button{Text=removing?"Desinstalar":"Instalar Zilda",BackColor=accent,ForeColor=back,FlatStyle=FlatStyle.Flat,Font=new Font("Segoe UI",10,FontStyle.Bold),Bounds=new Rectangle(370,434,198,36)};action.FlatAppearance.BorderSize=0;Controls.Add(action);var cancel=new Button{Text="Cancelar",FlatStyle=FlatStyle.Flat,ForeColor=muted,Bounds=new Rectangle(246,434,108,36)};cancel.FlatAppearance.BorderSize=0;cancel.Click+=delegate{Close();};Controls.Add(cancel);
                action.Click+=delegate{action.Enabled=false;cancel.Enabled=false;Cursor=Cursors.WaitCursor;try{if(uninstall){Uninstall(remove.Checked);status.Text="Zilda removida. Atalhos originais restaurados.";}else{Install(startup.Checked,launch.Checked);status.Text="Tudo pronto! A Zilda foi instalada.";}action.Text="Concluir";action.Enabled=true;action.Click+=delegate{Close();};if(startup!=null){startup.Enabled=false;launch.Enabled=false;}action.Click-=PerformAgain;Close();}catch(Exception ex){status.Text="Não foi possível concluir.";MessageBox.Show(this,ex.Message,"Zilda Organizadora",MessageBoxButtons.OK,MessageBoxIcon.Error);action.Enabled=true;cancel.Enabled=true;}finally{Cursor=Cursors.Default;}};
                if(args.Contains("--preview-output")){var timer=new System.Windows.Forms.Timer{Interval=1600};timer.Tick+=delegate{timer.Stop();using(var bitmap=new Bitmap(Width,Height)){DrawToBitmap(bitmap,new Rectangle(0,0,Width,Height));bitmap.Save(Value("--preview-output","setup.png"),System.Drawing.Imaging.ImageFormat.Png);}Close();};Shown+=delegate{timer.Start();};}
            }
            void PerformAgain(object s,EventArgs e){}
            Label AddLabel(string text,float size,FontStyle style,Color color,int x,int y,int w,int h){var label=new Label{Text=text,Font=new Font("Segoe UI",size,style),ForeColor=color,Bounds=new Rectangle(x,y,w,h)};Controls.Add(label);return label;}
        }
    }
}
