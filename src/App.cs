using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using System.Windows.Media;
using System.Runtime.InteropServices;

namespace Zilda {
    public class AppCore {
        public static AppCore Current;
        public Store Store;
        public Preferences Options {get{return Store.Active.Options;}}
        public ManagerWindow Manager;
        public bool Exiting;
        List<DockWindow> docks=new List<DockWindow>();
        Dictionary<string,PanelWindow> panels=new Dictionary<string,PanelWindow>();
        System.Windows.Forms.NotifyIcon tray;
        System.Windows.Application app;
        DispatcherTimer health;
        EventWaitHandle request;
        IntPtr managerHandle;
        public AppCore(Store store,EventWaitHandle signal){Current=this;Store=store;request=signal;}
        public void Run(bool minimized,bool smoke) {
            app=new System.Windows.Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};
            app.DispatcherUnhandledException+=delegate(object sender,DispatcherUnhandledExceptionEventArgs e){Store.Log(e.Exception);e.Handled=true;MessageBox.Show("Não foi possível concluir a operação.\n\n"+e.Exception.Message,"Zilda Organizadora",MessageBoxButton.OK,MessageBoxImage.Warning);};
            Manager=new ManagerWindow(this);Manager.SourceInitialized+=delegate{managerHandle=new WindowInteropHelper(Manager).Handle;HwndSource.FromHwnd(managerHandle).AddHook(Hotkey);Native.RegisterHotKey(managerHandle,1,0x0001|0x0002|0x4000,0x5A);};
            tray=new System.Windows.Forms.NotifyIcon{Text="Zilda Organizadora",Visible=true};
            try{tray.Icon=System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);}catch{tray.Icon=System.Drawing.SystemIcons.Application;}
            tray.DoubleClick+=delegate{ShowManager(null);};BuildTray();
            app.Startup+=delegate{
                RebuildDocks();
                if(!minimized)Manager.Show();else{new WindowInteropHelper(Manager).EnsureHandle();}
                if(!String.IsNullOrEmpty(Store.Warning))MessageBox.Show(Store.Warning,"Recuperação de dados da Zilda");
                if(smoke){var t=new DispatcherTimer{Interval=TimeSpan.FromSeconds(12)};t.Tick+=delegate{t.Stop();CaptureSmoke();Exit();};t.Start();}
            };
            health=new DispatcherTimer{Interval=TimeSpan.FromSeconds(2)};health.Tick+=delegate{try{
                if(request!=null&&request.WaitOne(0)){string f=Path.Combine(Store.Root,"solicitacao.txt");if(File.Exists(f)){string profile=File.ReadAllText(f);File.Delete(f);if(profile=="@restore-and-quit"){Store.RestoreAll();Exit();return;}if(profile=="@quit"){Exit();return;}var p=Store.Data.Profiles.FirstOrDefault(x=>String.Equals(x.Name,profile,StringComparison.OrdinalIgnoreCase));if(p!=null)Switch(p.Id);}ShowManager(null);}
                bool lost=docks.Any(d=>!Native.IsWindow(new WindowInteropHelper(d).Handle));
                if(lost)RebuildDocks();
                else if(docks.Any(d=>!d.Attached)&&Native.DesktopHost()!=IntPtr.Zero)foreach(var d in docks)d.Attached=Native.AttachDesktop(d,d.Group.X,d.Group.Y);
            }catch(Exception ex){Store.Log(ex);}};health.Start();
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged+=DisplayChanged;
            app.Run();
        }
        void CaptureSmoke(){
            try {string folder=Path.Combine(Store.Root,"smoke");Directory.CreateDirectory(folder);var source=PresentationSource.FromVisual(Manager);if(source!=null){var dpi=source.CompositionTarget.TransformToDevice;var image=new System.Windows.Media.Imaging.RenderTargetBitmap((int)(Manager.ActualWidth*dpi.M11),(int)(Manager.ActualHeight*dpi.M22),96*dpi.M11,96*dpi.M22,PixelFormats.Pbgra32);image.Render(Manager);var encoder=new System.Windows.Media.Imaging.PngBitmapEncoder();encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(image));using(var fs=File.Create(Path.Combine(folder,"gerenciador.png")))encoder.Save(fs);}
                File.WriteAllText(Path.Combine(folder,"result.txt"),"manager="+Manager.IsVisible+"\ndocks="+docks.Count+"\nattached="+docks.Count(d=>d.Attached)+"\npanels="+panels.Count+"\nprofile="+Store.Active.Name);
                foreach(string view in new[]{Store.Active.Groups[0].Id,"settings","profiles","about"}){Manager.Select(view);Manager.UpdateLayout();RenderWindow(Manager,Path.Combine(folder,view==Store.Active.Groups[0].Id?"grupo.png":view+".png"));}
                var group=Store.Active.Groups[0];
                int animation=Options.Animation;Options.Animation=0;
                for(int i=1;i<=18;i++)Store.Add(group,"https://example.org/atalho-"+i,false);
                string probe=Path.Combine(Store.Root,"LauncherProbe.exe");if(File.Exists(probe)){var entry=Store.Add(group,probe,false);if(entry!=null)Launch(entry);}
                foreach(string layout in Zilda.Store.Layouts){group.Layout=layout;RebuildDocks();Open(docks[0]);var panel=panels[group.Id];panel.UpdateLayout();RenderWindow(panel,Path.Combine(folder,"layout-"+layout+".png"));File.AppendAllText(Path.Combine(folder,"result.txt"),"\n"+layout+"="+panel.ActualWidth+"x"+panel.ActualHeight);ClosePanel(group.Id);}
                Options.OpenMode="Clique";Options.SingleOpen=false;Open(docks[0]);Open(docks[1]);File.AppendAllText(Path.Combine(folder,"result.txt"),"\nclick-multi="+(panels.Count==2));
                Options.SingleOpen=true;Open(docks[2]);File.AppendAllText(Path.Combine(folder,"result.txt"),"\nclick-single="+(panels.Count==1));
                Options.OpenMode="Hover";Options.SingleOpen=false;Open(docks[0]);Open(docks[1]);File.AppendAllText(Path.Combine(folder,"result.txt"),"\nhover-single="+(panels.Count==1));
                foreach(string id in panels.Keys.ToArray())ClosePanel(id);Options.SingleOpen=true;Options.Animation=animation;Store.Save();
                Manager.Select(group.Id);Manager.UpdateLayout();RenderWindow(Manager,Path.Combine(folder,"grupo-com-atalhos.png"));
            }catch(Exception ex){Store.Log(ex);}
        }
        static void RenderWindow(Window window,string path){var source=PresentationSource.FromVisual(window);if(source==null)return;var dpi=source.CompositionTarget.TransformToDevice;var image=new System.Windows.Media.Imaging.RenderTargetBitmap(Math.Max(1,(int)(window.ActualWidth*dpi.M11)),Math.Max(1,(int)(window.ActualHeight*dpi.M22)),96*dpi.M11,96*dpi.M22,PixelFormats.Pbgra32);image.Render(window);var enc=new System.Windows.Media.Imaging.PngBitmapEncoder();enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(image));using(var file=File.Create(path))enc.Save(file);}
        IntPtr Hotkey(IntPtr hwnd,int msg,IntPtr wp,IntPtr lp,ref bool handled){if(msg==0x0312&&wp.ToInt32()==1){ShowManager(null);handled=true;}return IntPtr.Zero;}
        void DisplayChanged(object s,EventArgs e){if(app!=null)app.Dispatcher.BeginInvoke(new Action(delegate{RebuildDocks();}));}
        public void BuildTray(){
            if(tray==null)return;var menu=new System.Windows.Forms.ContextMenuStrip();menu.Items.Add("Abrir gerenciador",null,delegate{ShowManager(null);});menu.Items.Add("Mostrar grupos",null,delegate{RebuildDocks();});menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
            foreach(var profile in Store.Data.Profiles){var p=profile;var m=new System.Windows.Forms.ToolStripMenuItem(p.Name){Checked=p.Id==Store.Data.ActiveId};m.Click+=delegate{Switch(p.Id);};menu.Items.Add(m);}
            menu.Items.Add("Gerenciar perfis…",null,delegate{ShowManager("profiles");});menu.Items.Add("Configurações…",null,delegate{ShowManager("settings");});menu.Items.Add("Abrir arquivo de atalhos",null,delegate{Native.Shell(Path.Combine(Store.Root,"arquivo"));});menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());menu.Items.Add("Sair da Zilda",null,delegate{Exit();});var old=tray.ContextMenuStrip;tray.ContextMenuStrip=menu;if(old!=null)old.Dispose();
        }
        public void ShowManager(string id){if(Manager==null)return;if(id!=null)Manager.Select(id);Manager.Show();if(Manager.WindowState==WindowState.Minimized)Manager.WindowState=WindowState.Normal;Manager.Activate();}
        public void Switch(string profileId){if(Store.Data.Profiles.All(p=>p.Id!=profileId))return;Store.Data.ActiveId=profileId;Store.Save();RebuildDocks();if(Manager!=null)Manager.Select("home");BuildTray();}
        public void RebuildDocks(){
            var expanded=new HashSet<string>(panels.Keys);foreach(var p in panels.Values.ToList()){p.Stop();try{p.Close();}catch{}}panels.Clear();foreach(var d in docks){d.Stop();try{d.Close();}catch{}}docks.Clear();
            foreach(var g in Store.Active.Groups){Clamp(g,false);var d=new DockWindow(this,g);docks.Add(d);d.Show();if(expanded.Contains(g.Id)||(Options.RestoreExpanded&&g.Expanded))Open(d);}
        }
        public void Clamp(Group g,bool snap){
            var point=new System.Drawing.Point((int)g.X,(int)g.Y);var screen=System.Windows.Forms.Screen.FromPoint(point);var bounds=screen.WorkingArea;
            if(snap){g.X=bounds.Left+Math.Round((g.X-bounds.Left)/96)*96;g.Y=bounds.Top+Math.Round((g.Y-bounds.Top)/96)*96;}
            g.X=Math.Max(bounds.Left,Math.Min(g.X,bounds.Right-90));g.Y=Math.Max(bounds.Top,Math.Min(g.Y,bounds.Bottom-90));
        }
        public void FreePosition(Group g){var b=System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;for(int x=b.Left+260;x<b.Right-90;x+=96)for(int y=b.Top+90;y<b.Bottom-90;y+=96){if(Store.Active.Groups.All(a=>Math.Abs(a.X-x)>70||Math.Abs(a.Y-y)>70)){g.X=x;g.Y=y;return;}}g.X=b.Left+260;g.Y=b.Top+90;}
        public void Toggle(DockWindow d){if(panels.ContainsKey(d.Group.Id))ClosePanel(d.Group.Id);else Open(d);}
        public void OpenGroup(Group g){var d=docks.FirstOrDefault(x=>x.Group.Id==g.Id);if(d!=null){Open(d);Manager.WindowState=WindowState.Minimized;}}
        public void Open(DockWindow d){
            PanelWindow existing;if(panels.TryGetValue(d.Group.Id,out existing)){existing.CancelClose();return;}
            if(Options.SingleOpen||Options.OpenMode=="Hover")foreach(string id in panels.Keys.ToArray())ClosePanel(id);
            var p=new PanelWindow(this,d);panels.Add(d.Group.Id,p);d.Group.Expanded=true;p.Show();p.Animate();Store.Save();
        }
        public void ScheduleClose(DockWindow d){PanelWindow p;if(panels.TryGetValue(d.Group.Id,out p))p.Schedule();}
        public void ClosePanel(string id){PanelWindow p;if(panels.TryGetValue(id,out p)){p.Stop();panels.Remove(id);p.Close();var g=Store.Active.Groups.FirstOrDefault(x=>x.Id==id);if(g!=null)g.Expanded=false;Store.Save();}}
        public void Drop(Group g,DragEventArgs e){
            if(e.Data.GetDataPresent("ZildaEntry")){e.Handled=true;return;}
            try{int count=0;if(e.Data.GetDataPresent(DataFormats.FileDrop)){foreach(string file in (string[])e.Data.GetData(DataFormats.FileDrop))if(Store.Add(g,file,true)!=null)count++;}
                else if(e.Data.GetDataPresent(DataFormats.UnicodeText)){string text=(string)e.Data.GetData(DataFormats.UnicodeText);if(Store.Add(g,text,false)!=null)count++;}
                e.Handled=true;RebuildDocks();if(Manager!=null){Manager.Refresh();Manager.Notice(count+" atalho(s) adicionado(s).");}
            }catch(Exception ex){e.Handled=true;Error(ex);}
        }
        public void Launch(Entry entry){string target=Store.Resolve(entry);if(!File.Exists(target)&&!Directory.Exists(target)&&!(target.StartsWith("https://",StringComparison.OrdinalIgnoreCase)||target.StartsWith("http://",StringComparison.OrdinalIgnoreCase)))throw new FileNotFoundException("O destino não está mais disponível. Adicione o arquivo novamente.",target);Native.Shell(target);}
        public void LaunchAll(Group g){if(g.Entries.Count>6&&!UI.Confirm(Manager,"Abrir os "+g.Entries.Count+" atalhos deste grupo?"))return;foreach(var e in g.Entries.ToList())try{Launch(e);}catch(Exception ex){Error(ex);}}
        public void ToDesktop(Group g,Entry e){
            if(!String.IsNullOrEmpty(e.Archived)&&File.Exists(Store.Local(e.Archived))){Store.Remove(g,e);return;}
            string desktop=Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);Directory.CreateDirectory(desktop);
            string name=e.Name;foreach(char c in Path.GetInvalidFileNameChars())name=name.Replace(c,'_');if(name.Length>100)name=name.Substring(0,100);
            string resolved=Store.Resolve(e),ext=Path.GetExtension(resolved).ToLowerInvariant();
            if(ext==".lnk"||ext==".url"){File.Copy(resolved,Store.Unique(Path.Combine(desktop,name+ext)),false);}
            else if(resolved.StartsWith("http://")||resolved.StartsWith("https://")){File.WriteAllText(Store.Unique(Path.Combine(desktop,name+".url")),"[InternetShortcut]\r\nURL="+resolved+"\r\n");}
            else {Type t=Type.GetTypeFromProgID("WScript.Shell");object shell=Activator.CreateInstance(t),shortcut=null;try{shortcut=t.InvokeMember("CreateShortcut",System.Reflection.BindingFlags.InvokeMethod,null,shell,new object[]{Store.Unique(Path.Combine(desktop,name+".lnk"))});shortcut.GetType().InvokeMember("TargetPath",System.Reflection.BindingFlags.SetProperty,null,shortcut,new object[]{resolved});shortcut.GetType().InvokeMember("Save",System.Reflection.BindingFlags.InvokeMethod,null,shortcut,null);}finally{if(shortcut!=null)Marshal.FinalReleaseComObject(shortcut);Marshal.FinalReleaseComObject(shell);}}
            Store.Remove(g,e);
        }
        public void Error(Exception ex){Store.Log(ex);if(Manager!=null)MessageBox.Show(Manager,ex.Message,"Zilda Organizadora",MessageBoxButton.OK,MessageBoxImage.Warning);else MessageBox.Show(ex.Message,"Zilda Organizadora");}
        public void Exit(){if(Exiting)return;Exiting=true;Store.Save();if(health!=null)health.Stop();Microsoft.Win32.SystemEvents.DisplaySettingsChanged-=DisplayChanged;if(managerHandle!=IntPtr.Zero)Native.UnregisterHotKey(managerHandle,1);foreach(var p in panels.Values){p.Stop();p.Close();}foreach(var d in docks){d.Stop();d.Close();}if(tray!=null){tray.Visible=false;tray.Dispose();}app.Shutdown();}
    }
    public static class Program {
        [STAThread] public static int Main(string[] args){
            string root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"ZildaOrganizadora");
            int data=Array.IndexOf(args,"--data-dir");if(data>=0&&data+1<args.Length)root=args[data+1];
            if(args.Contains("--self-test"))return SelfTests.Run(root);
            try {
                string digest;using(var sha=System.Security.Cryptography.SHA256.Create())digest=BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(Path.GetFullPath(root).ToLowerInvariant()))).Replace("-","").Substring(0,20);
                bool created;using(var mutex=new Mutex(true,"Local\\ZildaOrganizadora-"+digest,out created))using(var signal=new EventWaitHandle(false,EventResetMode.AutoReset,"Local\\ZildaOrganizadoraSignal-"+digest)){
                    int pi=Array.IndexOf(args,"--profile");string profile=pi>=0&&pi+1<args.Length?args[pi+1]:"";
                    bool restore=args.Contains("--restore-archives"),quit=args.Contains("--quit");
                    if(!created){if(profile!=""||restore||quit){Directory.CreateDirectory(root);File.WriteAllText(Path.Combine(root,"solicitacao.txt"),restore?"@restore-and-quit":quit?"@quit":profile);}signal.Set();if(restore||quit){bool stopped=false;try{stopped=mutex.WaitOne(TimeSpan.FromSeconds(20));}catch(AbandonedMutexException){stopped=true;}if(!stopped)return 2;mutex.ReleaseMutex();}return 0;}
                    if(quit&&!restore)return 0;
                    var store=new Store(root);
                    if(restore){store.RestoreAll();return 0;}
                    if(profile!=""){var p=store.Data.Profiles.FirstOrDefault(x=>String.Equals(x.Name,profile,StringComparison.OrdinalIgnoreCase));if(p!=null){store.Data.ActiveId=p.Id;store.Save();}}
                    var core=new AppCore(store,signal);core.Run(args.Contains("--tray"),args.Contains("--smoke"));mutex.ReleaseMutex();
                }return 0;
            }catch(Exception ex){try{Directory.CreateDirectory(root);File.AppendAllText(Path.Combine(root,"zilda.log"),ex.ToString()+Environment.NewLine);}catch{}MessageBox.Show("A Zilda não conseguiu iniciar.\n\n"+ex.Message,"Zilda Organizadora",MessageBoxButton.OK,MessageBoxImage.Error);return 1;}
        }
    }
}
