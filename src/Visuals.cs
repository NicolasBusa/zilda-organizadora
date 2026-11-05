using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Input;
using System.Windows.Threading;
using System.IO;
using System.Windows.Interop;

namespace Zilda {
    public class Plan {
        public double Width, Height, CenterX, CenterY, Radius;
        public List<Point> Points = new List<Point>();
        public static Plan Make(string layout,int count,Preferences o) {
            Plan p=new Plan(); double cell=82, tile=74; int n=Math.Max(count,1);
            if(layout=="Anel"||layout=="Leque") {
                double span=layout=="Anel"?2*Math.PI:Math.PI;
                p.Radius=Math.Max(106, (n*cell)/span);
                p.Width=p.Radius*2+tile+24;
                p.Height=layout=="Anel"?p.Width:p.Radius+tile+32;
                p.CenterX=p.Width/2; p.CenterY=layout=="Anel"?p.Height/2:p.Height-26;
                for(int i=0;i<count;i++) { double a=layout=="Anel"?-Math.PI/2+2*Math.PI*i/n:-Math.PI+(Math.PI*(i+.5)/n);p.Points.Add(new Point(p.CenterX+Math.Cos(a)*p.Radius-tile/2,p.CenterY+Math.Sin(a)*p.Radius-tile/2)); }
            } else {
                int cols=layout=="Coluna"?1:layout=="Linha"?Math.Min(n,8):Math.Min(n,o.Columns);
                int rows=(int)Math.Ceiling((double)n/cols);
                p.Width=cols*cell+18; p.Height=rows*cell+18;
                for(int i=0;i<count;i++)p.Points.Add(new Point(12+(i%cols)*cell,12+(i/cols)*cell));
            }
            return p;
        }
    }
    public static class UI {
        public static Brush Ink=new SolidColorBrush(Color.FromRgb(235,237,246));
        public static Brush Muted=new SolidColorBrush(Color.FromRgb(147,156,178));
        public static Brush Back=new SolidColorBrush(Color.FromRgb(16,19,28));
        public static Brush Surface=new SolidColorBrush(Color.FromRgb(25,29,41));
        public static Brush Edge=new SolidColorBrush(Color.FromRgb(45,50,68));
        public static Brush Accent=new SolidColorBrush(Color.FromRgb(160,135,255));
        public static Brush Brush(string s) { try{return (Brush)new BrushConverter().ConvertFromString(s);}catch{return Accent;} }
        public static TextBlock Text(string t,double size,Brush color) { return new TextBlock{Text=t,FontSize=size,Foreground=color,TextWrapping=TextWrapping.Wrap,VerticalAlignment=VerticalAlignment.Center}; }
        public static StackPanel Stack(params UIElement[] children) {var s=new StackPanel();foreach(var c in children)s.Children.Add(c);return s;}
        public static StackPanel Row(params UIElement[] children) {var s=Stack(children);s.Orientation=Orientation.Horizontal;return s;}
        public static Border Card(UIElement child,Thickness padding) {return new Border{Background=Surface,BorderBrush=Edge,BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(14),Padding=padding,Child=child,Margin=new Thickness(0,0,0,16)};}
        public static Button Button(string text,Action action,bool primary=false) {
            var b=new Button{Content=text,Foreground=primary?Brush("#191325"):Ink,Background=primary?Accent:Brush("#262C3D"),BorderThickness=new Thickness(0),Padding=new Thickness(14,9,14,9),Margin=new Thickness(0,0,8,0),Cursor=Cursors.Hand,FontSize=12,FontWeight=FontWeights.SemiBold,MinHeight=34};
            var template=new ControlTemplate(typeof(Button)); var bd=new FrameworkElementFactory(typeof(Border)); bd.SetValue(Border.CornerRadiusProperty,new CornerRadius(8));bd.SetValue(Border.BackgroundProperty,new TemplateBindingExtension(Control.BackgroundProperty));bd.SetValue(Border.PaddingProperty,new TemplateBindingExtension(Control.PaddingProperty));var presenter=new FrameworkElementFactory(typeof(ContentPresenter));presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty,HorizontalAlignment.Center);presenter.SetValue(ContentPresenter.VerticalAlignmentProperty,VerticalAlignment.Center);bd.AppendChild(presenter);template.VisualTree=bd;b.Template=template;
            b.MouseEnter+=delegate{b.Opacity=.82;};b.MouseLeave+=delegate{b.Opacity=1;};b.Click+=delegate{try{action();}catch(Exception ex){AppCore.Current.Error(ex);}};return b;
        }
        public static UIElement GroupIcon(Group g,int size) {
            if(!String.IsNullOrEmpty(g.Icon)){string path=g.Icon;if(!Path.IsPathRooted(path)&&AppCore.Current!=null)path=AppCore.Current.Store.Local(path);ImageSource icon=Native.Icon(path,size);if(icon!=null)return new Image{Source=icon,Width=size,Height=size};}
            var grid=new Grid{Width=size,Height=size};
            grid.Children.Add(new Border{CornerRadius=new CornerRadius(size*.25),Background=Brush(g.Color)});
            grid.Children.Add(new Border{CornerRadius=new CornerRadius(size*.25),Background=new LinearGradientBrush(Color.FromArgb(70,255,255,255),Color.FromArgb(0,255,255,255),90)});
            grid.Children.Add(new TextBlock{Text=g.Symbol,FontFamily=new FontFamily("Segoe UI Symbol"),FontSize=size*.58,Foreground=Brush("#FFFFFF"),HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center});return grid;
        }
        public static UIElement EntryIcon(Store store,Entry e,int size) {
            if(e.Target.StartsWith("http://",StringComparison.OrdinalIgnoreCase)||e.Target.StartsWith("https://",StringComparison.OrdinalIgnoreCase))return GroupIcon(new Group{Symbol="↗",Color="#5A98DE"},size);
            var icon=Native.Icon(store.Resolve(e),size);
            if(icon!=null)return new Image{Source=icon,Width=size,Height=size};
            return new Border{Width=size,Height=size,CornerRadius=new CornerRadius(9),Background=Brush("#35425D"),Child=new TextBlock{Text=e.Target.StartsWith("http")?"↗":"▤",Foreground=Ink,FontSize=size*.6,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center}};
        }
        public static Button Tile(UIElement icon,string name,Action action,bool light=false) {
            var label=new TextBlock{Text=name,FontSize=11,Foreground=light?Brush("#202535"):Ink,TextAlignment=TextAlignment.Center,TextTrimming=TextTrimming.CharacterEllipsis,MaxWidth=72,Margin=new Thickness(0,6,0,0),TextWrapping=TextWrapping.NoWrap};
            var content=Stack(icon,label); content.HorizontalAlignment=HorizontalAlignment.Center;
            var b=Button("",action); b.Content=content;b.Width=74;b.Height=74;b.Padding=new Thickness(2);b.Margin=new Thickness(0);b.Background=Brush("#001E2230");b.ToolTip=name;return b;
        }
        public static string Ask(Window owner,string title,string current) {
            var w=new Window{Title=title,Width=440,Height=196,ResizeMode=ResizeMode.NoResize,WindowStartupLocation=WindowStartupLocation.CenterOwner,Owner=owner,Background=Back,Foreground=Ink,FontFamily=new FontFamily("Segoe UI"),ShowInTaskbar=false};
            var box=new TextBox{Text=current,FontSize=15,Padding=new Thickness(10),Background=Surface,Foreground=Ink,BorderBrush=Edge,Margin=new Thickness(0,12,0,16)};
            string result=null;
            var ok=Button("Salvar",delegate{if(!String.IsNullOrWhiteSpace(box.Text)){result=box.Text.Trim();w.DialogResult=true;}},true);ok.IsDefault=true;
            var cancel=Button("Cancelar",delegate{w.Close();});cancel.IsCancel=true;
            w.Content=new Border{Padding=new Thickness(20),Child=Stack(Text(title,15,Ink),box,Row(ok,cancel))};
            w.Loaded+=delegate{box.Focus();box.SelectAll();};w.ShowDialog();return result;
        }
        public static bool Confirm(Window owner,string text) {return MessageBox.Show(owner,text,"Zilda Organizadora",MessageBoxButton.YesNo,MessageBoxImage.Question)==MessageBoxResult.Yes;}
    }
    public class DockWindow:Window {
        public readonly Group Group;
        AppCore app;
        DispatcherTimer hover;
        bool moving;
        Native.POINT start;
        double sx,sy;
        public bool Attached;
        public DockWindow(AppCore a,Group g) {
            app=a;Group=g;Width=90;Height=90;WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;AllowsTransparency=true;Background=Brushes.Transparent;ShowInTaskbar=false;ShowActivated=false;Title="Zilda • "+g.Name;
            var tile=UI.Tile(UI.GroupIcon(g,app.Options.IconSize),g.Name,delegate {if(!moving)app.Toggle(this);});
            tile.Height=86;tile.Width=86;
            if(app.Options.Highlight){tile.MouseEnter+=delegate{tile.Background=UI.Brush("#334D597C");};tile.MouseLeave+=delegate{tile.Background=Brushes.Transparent;};}
            Content=tile;Left=g.X;Top=g.Y;
            hover=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(Math.Max(1,app.Options.OpenDelay))};hover.Tick+=delegate{hover.Stop();if(!moving&&IsMouseOver)app.Open(this);};
            MouseEnter+=delegate{if(app.Options.OpenMode=="Hover")hover.Start();};MouseLeave+=delegate{hover.Stop();app.ScheduleClose(this);};
            tile.PreviewMouseLeftButtonDown+=delegate(object sender,MouseButtonEventArgs e){Native.GetCursorPos(out start);sx=Group.X;sy=Group.Y;moving=false;if(e.ClickCount==2){hover.Stop();if(g.DoubleAction=="Abrir todos")app.LaunchAll(g);else if(g.DoubleAction=="Gerenciador")app.ShowManager(g.Id);e.Handled=true;}};
            tile.PreviewMouseMove+=delegate(object sender,MouseEventArgs e){if(e.LeftButton!=MouseButtonState.Pressed)return;Native.POINT pt;Native.GetCursorPos(out pt);if(!moving&&Math.Abs(pt.X-start.X)+Math.Abs(pt.Y-start.Y)>10){moving=true;hover.Stop();tile.CaptureMouse();app.ClosePanel(g.Id);}if(moving){g.X=sx+pt.X-start.X;g.Y=sy+pt.Y-start.Y;Native.Position(this,g.X,g.Y);}};
            tile.PreviewMouseLeftButtonUp+=delegate(object sender,MouseButtonEventArgs e){if(moving){tile.ReleaseMouseCapture();app.Clamp(g,app.Options.Snap);Native.Position(this,g.X,g.Y);app.Store.Save();moving=false;e.Handled=true;}};
            AllowDrop=true;DragEnter+=delegate{hover.Stop();app.Open(this);};Drop+=delegate(object sender,DragEventArgs e){app.Drop(g,e);};
            ContextMenu=new ContextMenu();ContextMenuOpening+=delegate{BuildMenu();};
            SourceInitialized+=delegate{Attached=Native.AttachDesktop(this,g.X,g.Y);};
        }
        void BuildMenu(){ContextMenu.Items.Clear();Menu("Abrir grupo",delegate{app.Open(this);});Menu("Editar grupo",delegate{app.ShowManager(Group.Id);});var styles=new MenuItem{Header="Estilo de expansão"};foreach(string style in Store.Layouts){string s=style;var m=new MenuItem{Header=s,IsCheckable=true,IsChecked=Group.Layout==s};m.Click+=delegate{Group.Layout=s;app.Store.Save();app.ClosePanel(Group.Id);};styles.Items.Add(m);}ContextMenu.Items.Add(styles);Menu("Renomear…",delegate{app.ShowManager(Group.Id);app.Manager.RenameGroup(Group);});Menu("Abrir todos os atalhos",delegate{app.LaunchAll(Group);});ContextMenu.Items.Add(new Separator());Menu("Gerenciador da Zilda",delegate{app.ShowManager(null);});Menu("Remover grupo…",delegate{app.ShowManager(Group.Id);app.Manager.DeleteGroup(Group);});}
        void Menu(string title,Action a){var m=new MenuItem{Header=title};m.Click+=delegate{try{a();}catch(Exception e){app.Error(e);}};ContextMenu.Items.Add(m);}
        public void Stop(){hover.Stop();}
    }
    public class PanelWindow:Window {
        AppCore app;DockWindow dock;
        DispatcherTimer closer;
        Canvas canvas;
        int page;
        public string GroupId {get{return dock.Group.Id;}}
        public PanelWindow(AppCore a,DockWindow d) {
            app=a;dock=d;WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;AllowsTransparency=true;Background=Brushes.Transparent;ShowInTaskbar=false;ShowActivated=false;Title="Zilda • atalhos de "+d.Group.Name;
            closer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(Math.Max(50,app.Options.CloseDelay))};closer.Tick+=delegate{closer.Stop();if(!IsMouseOver&&!dock.IsMouseOver&&!IsKeyboardFocusWithin)app.ClosePanel(GroupId);};
            MouseLeave+=delegate{Schedule();};MouseEnter+=delegate{closer.Stop();};KeyDown+=delegate(object s,KeyEventArgs e){if(e.Key==Key.Escape)app.ClosePanel(GroupId);};
            AllowDrop=true;Drop+=delegate(object s,DragEventArgs e){app.Drop(d.Group,e);};
            Build();SourceInitialized+=delegate{Native.AttachDesktop(this,Left,Top);};
        }
        public void Schedule(){if(app.Options.OpenMode=="Hover"||app.Options.CloseOnLeave)closer.Start();}
        public void CancelClose(){closer.Stop();}
        public void Stop(){closer.Stop();}
        public void Build() {
            Group g=dock.Group;Preferences o=app.Options;
            var area=System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point((int)dock.Group.X,(int)dock.Group.Y)).WorkingArea;
            var source=PresentationSource.FromVisual(dock);double scale=source==null?1:source.CompositionTarget.TransformToDevice.M11;
            double maxWidth=Math.Max(240,area.Width/scale-24),maxHeight=Math.Max(190,area.Height/scale-80);
            var layoutOptions=new Preferences{Columns=Math.Max(1,Math.Min(o.Columns,(int)((maxWidth-18)/82))),Rows=Math.Max(1,Math.Min(o.Rows,(int)((maxHeight-18)/82)))};
            int cap=g.Layout=="Anel"?16:g.Layout=="Leque"?10:Math.Max(1,layoutOptions.Columns*layoutOptions.Rows);
            if(g.Layout=="Coluna")cap=layoutOptions.Rows;if(g.Layout=="Linha")cap=Math.Max(1,Math.Min(8,(int)((maxWidth-18)/82)))*Math.Min(layoutOptions.Rows,3);
            if(g.Layout=="Anel"||g.Layout=="Leque")while(cap>1){var test=Plan.Make(g.Layout,cap,o);if(test.Width<=maxWidth&&test.Height<=maxHeight)break;cap--;}
            int pages=Math.Max(1,(int)Math.Ceiling((double)g.Entries.Count/cap));page=Math.Min(page,pages-1);
            var entries=g.Entries.Skip(page*cap).Take(cap).ToList();var plan=Plan.Make(g.Layout,entries.Count,layoutOptions);
            if(g.Layout=="Linha"&&plan.Width>maxWidth){layoutOptions.Columns=Math.Max(1,(int)((maxWidth-18)/82));plan=Plan.Make("Grade",entries.Count,layoutOptions);}
            bool light=o.Theme=="Claro"||(o.Theme=="Sistema"&&Native.SystemLight());
            canvas=new Canvas{Width=plan.Width,Height=plan.Height,Background=Brushes.Transparent};
            if(g.Layout=="Anel"||g.Layout=="Leque") {
                var fill=new SolidColorBrush(light?Color.FromArgb((byte)(255*o.Opacity),240,242,248):Color.FromArgb((byte)(255*o.Opacity),29,33,44));
                Geometry outside=new EllipseGeometry(new Point(plan.CenterX,plan.CenterY),plan.Radius+47,plan.Radius+47);
                Geometry inside=new EllipseGeometry(new Point(plan.CenterX,plan.CenterY),Math.Max(35,plan.Radius-48),Math.Max(35,plan.Radius-48));
                var ring=new CombinedGeometry(GeometryCombineMode.Exclude,outside,inside);
                var shape=new System.Windows.Shapes.Path{Data=ring,Fill=fill,Stroke=UI.Brush(light?"#CAD0E0":"#424859"),StrokeThickness=1,IsHitTestVisible=false};canvas.Children.Add(shape);canvas.ClipToBounds=true;
                if(g.Layout=="Anel"){var center=UI.Tile(UI.GroupIcon(g,o.IconSize),g.Name,delegate{app.ClosePanel(g.Id);},light);Canvas.SetLeft(center,plan.CenterX-37);Canvas.SetTop(center,plan.CenterY-37);canvas.Children.Add(center);}
            } else canvas.Background=Brushes.Transparent;
            for(int i=0;i<entries.Count;i++) {
                Entry entry=entries[i];int index=page*cap+i;
                var tile=UI.Tile(UI.EntryIcon(app.Store,entry,o.IconSize),entry.Name,delegate{app.Launch(entry);app.ClosePanel(g.Id);},light);
                tile.ToolTip=entry.Name+Environment.NewLine+entry.Target;
                tile.ContextMenu=EntryMenu(g,entry,index);
                Point dragStart=new Point();
                tile.PreviewMouseLeftButtonDown+=delegate(object s,MouseButtonEventArgs e){dragStart=e.GetPosition(tile);};
                tile.PreviewMouseMove+=delegate(object s,MouseEventArgs e){Point pos=e.GetPosition(tile);if(e.LeftButton==MouseButtonState.Pressed&&(Math.Abs(pos.X-dragStart.X)+Math.Abs(pos.Y-dragStart.Y)>12)){var data=new DataObject();data.SetData("ZildaEntry",entry.Id);string target=app.Store.Resolve(entry);if(File.Exists(target)||Directory.Exists(target))data.SetData(DataFormats.FileDrop,new[]{target});DragDrop.DoDragDrop(tile,data,DragDropEffects.Copy);}};
                tile.AllowDrop=true;tile.Drop+=delegate(object s,DragEventArgs e){if(e.Data.GetDataPresent("ZildaEntry")){string id=e.Data.GetData("ZildaEntry") as string;var from=g.Entries.FirstOrDefault(x=>x.Id==id);if(from!=null&&from!=entry){g.Entries.Remove(from);g.Entries.Insert(Math.Min(index,g.Entries.Count),from);app.Store.Save();Build();}e.Handled=true;}};
                Canvas.SetLeft(tile,plan.Points[i].X);Canvas.SetTop(tile,plan.Points[i].Y);canvas.Children.Add(tile);
            }
            if(entries.Count==0) {
                canvas.Width=270;canvas.Height=126;canvas.Children.Clear();
                var empty=UI.Stack(UI.Text("Seu grupo está pronto",16,light?UI.Brush("#202535"):UI.Ink),UI.Text("Arraste atalhos para cá ou adicione pelo gerenciador.",12,light?UI.Brush("#515B72"):UI.Muted));empty.Margin=new Thickness(20,18,20,10);canvas.Children.Add(empty);
            }
            var body=UI.Stack();
            if(g.Layout=="Anel"||g.Layout=="Leque"){body.Children.Add(canvas);}else{var card=new Border{CornerRadius=new CornerRadius(12),Background=new SolidColorBrush(light?Color.FromArgb((byte)(255*o.Opacity),240,242,248):Color.FromArgb((byte)(255*o.Opacity),29,33,44)),BorderBrush=UI.Brush(light?"#CAD0E0":"#424859"),BorderThickness=new Thickness(1),Child=canvas};body.Children.Add(card);}
            var footer=UI.Row(UI.Button("+",delegate{app.ShowManager(g.Id);app.Manager.AddFiles(g);}),UI.Button("Editar",delegate{app.ShowManager(g.Id);}),UI.Button("×",delegate{app.ClosePanel(g.Id);}));footer.HorizontalAlignment=HorizontalAlignment.Center;footer.Margin=new Thickness(0,6,0,0);
            if(pages>1){footer.Children.Insert(0,UI.Button("‹",delegate{page=(page+pages-1)%pages;Build();Reposition();}));var counter=UI.Text((page+1)+" / "+pages,11,UI.Muted);counter.Margin=new Thickness(0,0,12,0);footer.Children.Insert(1,counter);footer.Children.Add(UI.Button("›",delegate{page=(page+1)%pages;Build();Reposition();}));}
            body.Children.Add(footer);Content=body;Width=Math.Max(canvas.Width,220)+2;Height=canvas.Height+52;
            Reposition();
        }
        public void Reposition() {
            var area=System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point((int)dock.Group.X,(int)dock.Group.Y)).WorkingArea;
            var source=PresentationSource.FromVisual(dock);double scale=source==null?1:source.CompositionTarget.TransformToDevice.M11;
            double x=dock.Group.X+70*scale,y=dock.Group.Y+8*scale;
            x=Math.Max(area.Left,Math.Min(x,area.Right-Width*scale));y=Math.Max(area.Top,Math.Min(y,area.Bottom-Height*scale));
            Left=x;Top=y;
            if(IsLoaded)Native.Position(this,x,y);
        }
        ContextMenu EntryMenu(Group g,Entry e,int index) {
            var c=new ContextMenu();Action<string,Action> add=delegate(string text,Action a){var m=new MenuItem{Header=text};m.Click+=delegate{try{a();}catch(Exception ex){app.Error(ex);}};c.Items.Add(m);};
            add("Abrir",delegate{app.Launch(e);});add("Abrir localização",delegate{Native.Reveal(e.Target);});add("Renomear…",delegate{app.ShowManager(g.Id);string n=UI.Ask(app.Manager,"Nome do atalho",e.Name);if(n!=null){e.Name=n.Substring(0,Math.Min(260,n.Length));app.Store.Save();Build();app.Manager.Refresh();}});
            add("Usar ícone no grupo",delegate{app.Store.UseIcon(g,e);app.RebuildDocks();});
            add("Mover para cima",delegate{if(index>0){g.Entries.Remove(e);g.Entries.Insert(index-1,e);app.Store.Save();Build();}});
            add("Devolver à área de trabalho",delegate{app.ToDesktop(g,e);Build();});
            add("Remover do grupo",delegate{app.Store.Remove(g,e);Build();if(app.Manager!=null)app.Manager.Refresh();});return c;
        }
        public void Animate(){if(app.Options.Animation<=0)return;var duration=TimeSpan.FromMilliseconds(app.Options.Animation);BeginAnimation(OpacityProperty,new DoubleAnimation(0,1,duration));foreach(Button tile in canvas.Children.OfType<Button>()){double x=Canvas.GetLeft(tile),y=Canvas.GetTop(tile);var transform=new TranslateTransform();tile.RenderTransform=transform;var ease=new CubicEase{EasingMode=EasingMode.EaseOut};transform.BeginAnimation(TranslateTransform.XProperty,new DoubleAnimation(canvas.Width/2-x-37,0,duration){EasingFunction=ease});transform.BeginAnimation(TranslateTransform.YProperty,new DoubleAnimation(canvas.Height-40-y,0,duration){EasingFunction=ease});}}
    }
}
