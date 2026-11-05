using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using System.IO.Compression;

namespace Zilda {
    public class Entry {
        public string Id = Guid.NewGuid().ToString("N");
        public string Name = "Atalho";
        public string Target = "";
        public string Copy = "";
        public string Archived = "";
        public string Original = "";
    }
    public class Group {
        public string Id = Guid.NewGuid().ToString("N");
        public string Name = "Novo grupo";
        public string Symbol = "▦";
        public string Color = "#9173F8";
        public string Icon = "";
        public string Layout = "Grade";
        public string DoubleAction = "Gerenciador";
        public double X = 260, Y = 90;
        public bool Expanded = false;
        public List<Entry> Entries = new List<Entry>();
    }
    public class Preferences {
        public string OpenMode = "Hover";
        public string Theme = "Escuro";
        public int OpenDelay = 300, CloseDelay = 500, Animation = 160;
        public int Columns = 4, Rows = 4, IconSize = 40;
        public double Opacity = .94;
        public bool CloseOnLeave = true, SingleOpen = true, Highlight = true;
        public bool ArchiveDesktop = false, RestoreExpanded = false, Snap = true;
    }
    public class Profile {
        public string Id = Guid.NewGuid().ToString("N");
        public string Name = "Pessoal";
        public Preferences Options = new Preferences();
        public List<Group> Groups = new List<Group>();
    }
    public class State {
        public int Version = 1;
        public string ActiveId = "";
        public List<Profile> Profiles = new List<Profile>();
    }
    public class Store {
        public readonly string Root;
        public readonly string Desktop;
        public State Data;
        public string Warning = "";
        public Profile Active { get { return Data.Profiles.First(p => p.Id == Data.ActiveId); } }
        static JavaScriptSerializer Json() { return new JavaScriptSerializer { MaxJsonLength = 16000000, RecursionLimit = 80 }; }
        public Store(string root, string desktop = null) {
            Root = Path.GetFullPath(root);
            Desktop = desktop ?? Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            Directory.CreateDirectory(Root);
            Directory.CreateDirectory(Path.Combine(Root, "atalhos"));
            Directory.CreateDirectory(Path.Combine(Root, "arquivo"));
            string path = Path.Combine(Root, "config.json");
            try {
                if (File.Exists(path)) { Data = Json().Deserialize<State>(File.ReadAllText(path)); Validate(Data); }
            } catch (Exception ex) {
                Log(ex);
                string damaged = Path.Combine(Root, "config-danificado-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".json");
                if (File.Exists(path)) File.Copy(path, damaged, true);
                string backup = path + ".bak";
                Data = null;
                try { if (File.Exists(backup)) { Data = Json().Deserialize<State>(File.ReadAllText(backup)); Validate(Data); } } catch { Data = null; }
                Warning = "A configuração estava danificada. " + (Data == null ? "Um novo perfil foi criado; seus arquivos foram preservados." : "O último backup foi recuperado.");
            }
            if (Data == null) {
                Data = new State(); var p = new Profile(); Data.Profiles.Add(p); Data.ActiveId = p.Id;
                string[] names = { "Trabalho", "Criatividade", "Favoritos" };
                string[] colors = { "#6AA9FF", "#C786EC", "#66CCB0" };
                string[] symbols = { "▤", "✦", "★" };
                for (int i = 0; i < 3; i++) p.Groups.Add(new Group { Name = names[i], Color = colors[i], Symbol = symbols[i], X = 260 + i * 96, Y = 90 });
                Save();
            }
        }
        public static void Validate(State s) {
            if (s == null || s.Version != 1 || s.Profiles == null || s.Profiles.Count < 1 || s.Profiles.Count > 50) throw new InvalidDataException("Formato de backup incompatível.");
            var ids = new HashSet<string>();
            foreach (var p in s.Profiles) {
                if (p == null || String.IsNullOrWhiteSpace(p.Id) || !ids.Add(p.Id) || String.IsNullOrWhiteSpace(p.Name) || p.Name.Length > 120 || p.Options == null || p.Groups == null || p.Groups.Count > 200) throw new InvalidDataException("Perfil inválido.");
                var o = p.Options;
                if (!new[] { "Hover", "Clique" }.Contains(o.OpenMode) || !new[] { "Escuro", "Claro", "Sistema" }.Contains(o.Theme)) throw new InvalidDataException("Preferências inválidas.");
                if (o.Columns < 1 || o.Columns > 12 || o.Rows < 1 || o.Rows > 12 || o.IconSize < 24 || o.IconSize > 72 || o.Opacity < .15 || o.Opacity > 1 || Double.IsNaN(o.Opacity) || o.OpenDelay < 0 || o.OpenDelay > 5000 || o.CloseDelay < 0 || o.CloseDelay > 5000 || o.Animation < 0 || o.Animation > 2000) throw new InvalidDataException("Limites das preferências inválidos.");
                var gids = new HashSet<string>();
                foreach (var g in p.Groups) {
                    if (g == null || String.IsNullOrWhiteSpace(g.Id) || !gids.Add(g.Id) || String.IsNullOrWhiteSpace(g.Name) || g.Name.Length > 120 || !Layouts.Contains(g.Layout) || g.Entries == null || g.Entries.Count > 2000 || Double.IsNaN(g.X) || Double.IsInfinity(g.X) || Double.IsNaN(g.Y) || Double.IsInfinity(g.Y) || Math.Abs(g.X) > 100000 || Math.Abs(g.Y) > 100000) throw new InvalidDataException("Grupo inválido.");
                    if (!String.IsNullOrEmpty(g.Icon) && !Path.IsPathRooted(g.Icon)) { CheckRelative(g.Icon, "atalhos"); g.Icon = Normalize(g.Icon); }
                    var eids = new HashSet<string>();
                    foreach (var e in g.Entries) {
                        if (e == null || String.IsNullOrWhiteSpace(e.Id) || !eids.Add(e.Id) || String.IsNullOrWhiteSpace(e.Name) || e.Name.Length > 260 || e.Target == null || e.Target.Length > 8192) throw new InvalidDataException("Atalho inválido.");
                        CheckRelative(e.Copy, "atalhos"); CheckRelative(e.Archived, "arquivo");
                        e.Copy = Normalize(e.Copy); e.Archived = Normalize(e.Archived);
                    }
                }
            }
            if (!s.Profiles.Any(p => p.Id == s.ActiveId)) throw new InvalidDataException("Perfil ativo ausente.");
        }
        public static readonly string[] Layouts = { "Grade", "Coluna", "Linha", "Leque", "Anel" };
        static string Normalize(string path) { return String.IsNullOrEmpty(path) ? "" : path.Replace('/', '\\').ToLowerInvariant(); }
        static void CheckRelative(string path, string parent) {
            if (String.IsNullOrEmpty(path)) return;
            string normalized = path.Replace('/', '\\');
            if (Path.IsPathRooted(path) || normalized.Contains("..") || normalized.Contains(":" ) || !normalized.StartsWith(parent + "\\", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Caminho inseguro no backup.");
        }
        public string Local(string relative) {
            string result = Path.GetFullPath(Path.Combine(Root, relative));
            if (!result.StartsWith(Root.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Caminho fora da pasta de dados.");
            return result;
        }
        public void Save() {
            Validate(Data);
            string path = Path.Combine(Root, "config.json"), tmp = path + ".novo";
            File.WriteAllText(tmp, Json().Serialize(Data), new System.Text.UTF8Encoding(false));
            if (File.Exists(path)) File.Replace(tmp, path, path + ".bak", true); else File.Move(tmp, path);
        }
        public void Log(Exception ex) { try { File.AppendAllText(Path.Combine(Root, "zilda.log"), DateTime.Now.ToString("s") + " " + ex.ToString() + Environment.NewLine); } catch { } }
        public string Resolve(Entry e) { return String.IsNullOrEmpty(e.Copy) ? e.Target : Local(e.Copy); }
        public Entry Add(Group group, string path, bool desktopDrop) {
            if (group.Entries.Count >= 2000) throw new InvalidOperationException("Este grupo atingiu o limite de 2.000 atalhos.");
            path = path.Trim('"', ' ');
            if (path.Length > 8192) throw new InvalidDataException("O destino ultrapassa o limite de 8.192 caracteres.");
            Uri uri;
            bool web = Uri.TryCreate(path, UriKind.Absolute, out uri) && (uri.Scheme == "https" || uri.Scheme == "http");
            if (!web && !File.Exists(path) && !Directory.Exists(path)) throw new FileNotFoundException("Arquivo ou pasta não encontrado.", path);
            if (group.Entries.Any(e => String.Equals(e.Target, path, StringComparison.OrdinalIgnoreCase))) return null;
            var item = new Entry { Target = path, Name = web ? uri.Host : Path.GetFileNameWithoutExtension(path.TrimEnd('\\')) };
            if (String.IsNullOrWhiteSpace(item.Name)) item.Name = path;
            string ext = Path.GetExtension(path).ToLowerInvariant();
            if (!web && (ext == ".lnk" || ext == ".url")) {
                item.Copy = Path.Combine("atalhos", item.Id + ext);
                File.Copy(path, Local(item.Copy), false);
            }
            // Commit the copied entry before moving an original. A move can never orphan a shortcut.
            group.Entries.Add(item); try { Save(); } catch { group.Entries.Remove(item); throw; }
            if (!web && desktopDrop && Active.Options.ArchiveDesktop && (ext == ".lnk" || ext == ".url") && String.Equals(Path.GetDirectoryName(Path.GetFullPath(path)), Desktop, StringComparison.OrdinalIgnoreCase)) {
                item.Original = Path.GetFullPath(path);
                item.Archived = Path.Combine("arquivo", item.Id + ext);
                Save();
                try { MoveFile(path, Local(item.Archived)); } catch { item.Archived = ""; item.Original = ""; Save(); throw; }
                Save();
            }
            return item;
        }
        public static bool IsDesktopFile(string path) {
            return String.Equals(Path.GetDirectoryName(Path.GetFullPath(path)), Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), StringComparison.OrdinalIgnoreCase);
        }
        public static string Unique(string path) {
            if (!File.Exists(path) && !Directory.Exists(path)) return path;
            string dir = Path.GetDirectoryName(path), stem = Path.GetFileNameWithoutExtension(path), ext = Path.GetExtension(path);
            for (int i = 2; i < 10000; i++) { string p = Path.Combine(dir, stem + " (" + i + ")" + ext); if (!File.Exists(p) && !Directory.Exists(p)) return p; }
            throw new IOException("Não foi possível escolher um nome livre.");
        }
        public void Restore(Entry e) {
            if (String.IsNullOrEmpty(e.Archived) || !File.Exists(Local(e.Archived))) return;
            string desktop = Desktop;
            Directory.CreateDirectory(desktop);
            string filename = Path.GetFileName(e.Original);
            if (String.IsNullOrEmpty(filename) || !(filename.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase) || filename.EndsWith(".url", StringComparison.OrdinalIgnoreCase))) filename = e.Name + Path.GetExtension(e.Archived);
            MoveFile(Local(e.Archived), Unique(Path.Combine(desktop, filename)));
            e.Archived = ""; e.Original = "";
        }
        public void Remove(Group g, Entry e) { Restore(e); g.Entries.Remove(e); Save(); }
        public void RemoveGroup(Group g) { foreach (var e in g.Entries) Restore(e); Active.Groups.Remove(g); Save(); }
        public void RestoreAll() {
            foreach (var e in Data.Profiles.SelectMany(p => p.Groups).SelectMany(g => g.Entries)) Restore(e);
            // Configuration recovery can leave unreferenced originals. Preserve and return those too.
            string archive = Path.Combine(Root, "arquivo");
            foreach (string file in Directory.GetFiles(archive).Where(f => new[] { ".lnk", ".url" }.Contains(Path.GetExtension(f).ToLowerInvariant()))) {
                Directory.CreateDirectory(Desktop);
                MoveFile(file, Unique(Path.Combine(Desktop, "Atalho recuperado - " + Path.GetFileName(file))));
            }
            Save();
        }
        static void MoveFile(string source, string destination) {
            try { File.Move(source, destination); }
            catch (IOException) {
                if (!File.Exists(source) || File.Exists(destination)) throw;
                File.Copy(source, destination, false);
                try { File.Delete(source); } catch { File.Delete(destination); throw; }
            }
        }
        public void Backup(string dest) {
            Save();
            string temp = dest + ".novo";
            if (File.Exists(temp)) File.Delete(temp);
            using (var zip = ZipFile.Open(temp, ZipArchiveMode.Create)) {
                zip.CreateEntryFromFile(Path.Combine(Root, "config.json"), "config.json");
                foreach (var folder in new[] { "atalhos", "arquivo" })
                    foreach (string f in Directory.GetFiles(Path.Combine(Root, folder))) zip.CreateEntryFromFile(f, folder + "/" + Path.GetFileName(f));
            }
            if (File.Exists(dest)) File.Delete(dest); File.Move(temp, dest);
        }
        public void Import(string path) {
            string stage = Path.Combine(Root, "importacao-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(stage);
            try {
                State incoming;
                using (var zip = ZipFile.OpenRead(path)) {
                    if (zip.Entries.Count > 20000 || zip.Entries.Sum(e => e.Length) > 512L * 1024 * 1024) throw new InvalidDataException("Backup excede o limite de segurança (512 MB).");
                    var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var entry in zip.Entries) {
                        string n = entry.FullName.Replace('/', '\\');
                        if (n == "config.json") { } else if (n.StartsWith("atalhos\\") || n.StartsWith("arquivo\\")) { CheckRelative(n, n.StartsWith("atalhos") ? "atalhos" : "arquivo"); if (Path.GetDirectoryName(n) != "atalhos" && Path.GetDirectoryName(n) != "arquivo") throw new InvalidDataException("Pasta de backup inválida."); } else throw new InvalidDataException("Conteúdo desconhecido no backup.");
                        if (!seen.Add(n)) throw new InvalidDataException("Entrada duplicada no backup.");
                        string outpath = Path.GetFullPath(Path.Combine(stage, n));
                        if (!outpath.StartsWith(stage + "\\", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Caminho inseguro.");
                        Directory.CreateDirectory(Path.GetDirectoryName(outpath)); entry.ExtractToFile(outpath, false);
                    }
                }
                incoming = Json().Deserialize<State>(File.ReadAllText(Path.Combine(stage, "config.json"))); Validate(incoming);
                foreach (var e in incoming.Profiles.SelectMany(p => p.Groups).SelectMany(g => g.Entries)) {
                    if (!String.IsNullOrEmpty(e.Copy) && !File.Exists(Path.Combine(stage, e.Copy))) throw new InvalidDataException("Atalho ausente no backup: " + e.Name);
                    if (!String.IsNullOrEmpty(e.Archived) && !File.Exists(Path.Combine(stage, e.Archived))) { e.Archived = ""; e.Original = ""; }
                }
                Backup(Path.Combine(Root, "antes-da-importacao-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".zip"));
                RestoreAll();
                // Preserve original archives belonging to the previous state, with new IDs in the imported state.
                foreach (var folder in new[] { "atalhos", "arquivo" }) {
                    string src = Path.Combine(stage, folder); if (!Directory.Exists(src)) continue;
                    foreach (string f in Directory.GetFiles(src)) {
                        string old = Path.Combine(folder, Path.GetFileName(f)); string fresh = Path.Combine(folder, Guid.NewGuid().ToString("N") + Path.GetExtension(f));
                        File.Copy(f, Local(fresh), false);
                        foreach (var e in incoming.Profiles.SelectMany(p => p.Groups).SelectMany(g => g.Entries)) { if (String.Equals(e.Copy, old, StringComparison.OrdinalIgnoreCase)) e.Copy = fresh; if (String.Equals(e.Archived, old, StringComparison.OrdinalIgnoreCase)) e.Archived = fresh; }
                        foreach (var g in incoming.Profiles.SelectMany(p => p.Groups)) if (String.Equals(g.Icon, old, StringComparison.OrdinalIgnoreCase)) g.Icon = fresh;
                    }
                }
                Data = incoming; Save();
            } finally { if (Directory.Exists(stage)) Directory.Delete(stage, true); }
        }
        public Profile Clone(Profile source, string name) {
            if (Data.Profiles.Count >= 50) throw new InvalidOperationException("Limite de 50 perfis.");
            Profile p = Json().Deserialize<Profile>(Json().Serialize(source));
            p.Id = Guid.NewGuid().ToString("N"); p.Name = name;
            foreach (var g in p.Groups) { g.Id = Guid.NewGuid().ToString("N"); foreach (var e in g.Entries) { e.Id = Guid.NewGuid().ToString("N"); e.Archived = ""; e.Original = ""; } }
            Data.Profiles.Add(p); Save(); return p;
        }
        public void UseIcon(Group g, Entry e) {
            if (!String.IsNullOrEmpty(e.Copy)) g.Icon = e.Copy;
            else if (File.Exists(e.Target) || Directory.Exists(e.Target)) g.Icon = e.Target;
            else { g.Icon = ""; g.Symbol = "↗"; }
            Save();
        }
    }
}
