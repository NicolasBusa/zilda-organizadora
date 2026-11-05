using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using Microsoft.Win32;

namespace Zilda {
    public static class Native {
        public delegate bool EnumProc(IntPtr hwnd, IntPtr param);
        static IntPtr blockedHost;
        static bool attachmentLogged;
        [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern IntPtr FindWindow(string cls, string title);
        [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string cls, string title);
        [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc proc, IntPtr param);
        [DllImport("user32.dll",SetLastError=true)] public static extern IntPtr SetParent(IntPtr child, IntPtr parent);
        [DllImport("user32.dll")] public static extern IntPtr GetParent(IntPtr child);
        [DllImport("user32.dll", EntryPoint="GetWindowLongW")] public static extern int GetWindowLong(IntPtr hwnd, int index);
        [DllImport("user32.dll", EntryPoint="SetWindowLongW")] public static extern int SetWindowLong(IntPtr hwnd, int index, int value);
        [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int w, int h, uint flags);
        [DllImport("user32.dll")] public static extern bool ScreenToClient(IntPtr hwnd, ref POINT point);
        [DllImport("user32.dll")] public static extern bool GetCursorPos(out POINT point);
        [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr hwnd);
        [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetClassName(IntPtr hwnd,System.Text.StringBuilder name,int max);
        [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr hwnd,int command);
        [DllImport("user32.dll")] public static extern bool RegisterHotKey(IntPtr hwnd, int id, uint mods, uint key);
        [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr hwnd, int id);
        [DllImport("user32.dll")] public static extern IntPtr SendMessageTimeout(IntPtr hwnd, uint msg, IntPtr wp, IntPtr lp, uint flags, uint timeout, out IntPtr result);
        [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr hwnd, uint msg, IntPtr wp, IntPtr lp);
        [DllImport("shell32.dll", CharSet=CharSet.Unicode)] static extern IntPtr SHGetFileInfo(string path, uint attr, out SHFILEINFO info, uint size, uint flags);
        [DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr icon);
        [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X,Y; public POINT(int x,int y){X=x;Y=y;} }
        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] struct SHFILEINFO { public IntPtr Icon; public int Index; public uint Attributes; [MarshalAs(UnmanagedType.ByValTStr,SizeConst=260)] public string Name; [MarshalAs(UnmanagedType.ByValTStr,SizeConst=80)] public string Type; }
        public static IntPtr DesktopHost() {
            IntPtr view = IntPtr.Zero;
            EnumWindows(delegate(IntPtr h, IntPtr p) { IntPtr v = FindWindowEx(h,IntPtr.Zero,"SHELLDLL_DefView",null); if(v != IntPtr.Zero){view=v;return false;} return true; },IntPtr.Zero);
            return view;
        }
        public static bool AttachDesktop(Window win, double x, double y) {
            IntPtr h = new WindowInteropHelper(win).Handle, parent = DesktopHost();
            if (parent == IntPtr.Zero) return false;
            if(parent==blockedHost){KeepBehind(win);return false;}
            int style = GetWindowLong(h,-16); SetWindowLong(h,-16,(style | 0x40000000) & ~unchecked((int)0x80000000));
            SetParent(h,parent);
            int error = Marshal.GetLastWin32Error();
            if(GetParent(h)!=parent){SetWindowLong(h,-16,style);SetWindowPos(h,new IntPtr(1),(int)x,(int)y,0,0,0x0001|0x0010|0x0020);if(error==5)blockedHost=parent;if(!attachmentLogged&&AppCore.Current!=null){attachmentLogged=true;AppCore.Current.Store.Log(new InvalidOperationException("Desktop attachment unavailable. Win32="+error+". Using desktop z-order fallback."));}return false;}
            Position(win,x,y);
            return GetParent(h)==parent;
        }
        public static void KeepBehind(Window win){IntPtr h=new WindowInteropHelper(win).Handle;if(GetParent(h)==IntPtr.Zero){var name=new System.Text.StringBuilder(128);GetClassName(GetForegroundWindow(),name,128);if(name.ToString()=="Progman"||name.ToString()=="WorkerW"){ShowWindow(h,4);}SetWindowPos(h,new IntPtr(1),0,0,0,0,0x0001|0x0002|0x0010);}}
        public static void Position(Window win, double x, double y) {
            IntPtr h = new WindowInteropHelper(win).Handle, parent = GetParent(h);
            if(parent==IntPtr.Zero){win.Left=x;win.Top=y;return;}
            Matrix scale = PresentationSource.FromVisual(win).CompositionTarget.TransformToDevice;
            POINT pt = new POINT((int)Math.Round(x),(int)Math.Round(y)); ScreenToClient(parent,ref pt);
            SetWindowPos(h,IntPtr.Zero,pt.X,pt.Y,(int)Math.Ceiling(win.Width*scale.M11),(int)Math.Ceiling(win.Height*scale.M22),0x0010|0x0040);
        }
        public static ImageSource Icon(string path, int size) {
            try {
                string ext=Path.GetExtension(path).ToLowerInvariant();
                if(ext==".png"||ext==".jpg"||ext==".jpeg"||ext==".bmp"||ext==".ico") {
                    if(File.Exists(path)){var b=new BitmapImage(); b.BeginInit();b.CacheOption=BitmapCacheOption.OnLoad;b.UriSource=new Uri(Path.GetFullPath(path));b.DecodePixelWidth=size;b.EndInit();b.Freeze();return b;}
                }
                SHFILEINFO info; uint flags=0x100|0x0; if(!File.Exists(path)&&!Directory.Exists(path)) flags|=0x10;
                SHGetFileInfo(path,0x80,out info,(uint)Marshal.SizeOf(typeof(SHFILEINFO)),flags);
                if(info.Icon!=IntPtr.Zero){try{var src=Imaging.CreateBitmapSourceFromHIcon(info.Icon,Int32Rect.Empty,BitmapSizeOptions.FromWidthAndHeight(size,size));src.Freeze();return src;}finally{DestroyIcon(info.Icon);}}
            } catch {}
            return null;
        }
        public static bool StartupEnabled() { try { using(var k=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run")) return k!=null && k.GetValue("ZildaOrganizadora")!=null; } catch(System.Security.SecurityException) { return false; } catch(UnauthorizedAccessException) { return false; } }
        public static void Startup(bool enable) {
            using(var k=Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run")){
                if(enable)k.SetValue("ZildaOrganizadora","\""+System.Reflection.Assembly.GetExecutingAssembly().Location+"\" --tray"); else k.DeleteValue("ZildaOrganizadora",false);
            }
        }
        public static bool SystemLight() { try { using(var k=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize")) return k!=null && Convert.ToInt32(k.GetValue("AppsUseLightTheme",0))==1; }catch{return false;} }
        public static void Shell(string path) { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path){UseShellExecute=true}); }
        public static void Reveal(string path) { if(File.Exists(path)) System.Diagnostics.Process.Start("explorer.exe","/select,\""+path+"\""); else if(Directory.Exists(path))Shell(path); }
    }
}
