using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using Lotlab.PluginCommon.Updater;

namespace GardeningTracker.Packer 
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var g = new UpdateGenerater();

            var root = args.Length >= 1 ? args[0] : AppDomain.CurrentDomain.BaseDirectory;
            var entry = Path.Combine(root, "GardeningTracker.dll");

            var ver = FileVersionInfo.GetVersionInfo(entry).FileVersion;
            g.Generate(root, ver, "");

            PackZip(root, Path.Combine(root, "_update", $"GardeningTracker-{ver}.zip"));
        }

        static void PackZip(string rootDir, string outName)
        {
            using (var ms = new MemoryStream())
            {
                using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
                {
                    foreach (var dll in Directory.GetFiles(rootDir, "*.dll"))
                    {
                        archive.CreateEntryFromFile(dll, Path.GetFileName(dll));
                    }
                    foreach (var data in Directory.GetFiles(Path.Combine(rootDir, "data"), "*"))
                    {
                        archive.CreateEntryFromFile(data, "data/" + Path.GetFileName(data));
                    }
                }

                using (var fileStream = new FileStream(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, outName), FileMode.Create))
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    ms.CopyTo(fileStream);
                }
            }
        }
    }
}
