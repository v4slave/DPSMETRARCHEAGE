using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
public static class AppPaths {
    const string Revision="2026-10-26-autoidentity-2";
    public static string Data {get {
        if(Assembly.GetExecutingAssembly().GetManifestResourceInfo("Assets.zip")==null)return AppDomain.CurrentDomain.BaseDirectory;
        string custom=Environment.GetEnvironmentVariable("ARCHEAGE_DPS_DATA");
        return string.IsNullOrWhiteSpace(custom)?Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"ArcheAgeDPS"):Path.GetFullPath(custom);
    }}
    public static void Initialize(){
        using(var resource=Assembly.GetExecutingAssembly().GetManifestResourceStream("Assets.zip")){
            if(resource==null)return;Directory.CreateDirectory(Data);string stamp=Path.Combine(Data,"assets-version.txt");
            if(File.Exists(stamp)&&File.ReadAllText(stamp)==Revision&&File.Exists(Path.Combine(Data,"assets","skills.tsv")))return;
            using(var zip=new ZipArchive(resource,ZipArchiveMode.Read))foreach(var entry in zip.Entries){
                string destination=Path.GetFullPath(Path.Combine(Data,entry.FullName.Replace('/',Path.DirectorySeparatorChar)));
                string root=Path.GetFullPath(Data).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
                if(!destination.StartsWith(root,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Invalid asset path");
                if(entry.FullName.EndsWith("/")){Directory.CreateDirectory(destination);continue;}
                if(entry.FullName=="character-id.txt"||entry.FullName=="character-name.txt")continue;
                if(entry.FullName=="assets/extra-skills.tsv"&&File.Exists(destination))continue;
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                using(var input=entry.Open())using(var output=File.Create(destination))input.CopyTo(output);
            }
            File.WriteAllText(stamp,Revision,Encoding.ASCII);
        }
    }
}
