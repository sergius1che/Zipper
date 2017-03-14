using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Zipper
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
#if DEBUG
                Config conf = new Config(new string[] { "-C", @"C:\Repository\Vet\vetoffline\Vet.Setup\Debug\", @"\\192.168.0.2\public\Сотрудник Чечин Сергей\Vet" });
                //Config conf = new Config(args);
                foreach (var item in args)
                {
                    Console.WriteLine(item);
                }
#else
                Config conf = new Config(args);
#endif

                if (conf.Type == Config.COMPRESS)
                {
                    Compress(conf);
                }
                else if (conf.Type == Config.UNCOMPRESS)
                {
                    Uncompress(conf);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Config.GetHelp();
            }
        }

        public static void Compress(Config conf)
        {
            string name = conf.Source.Split('\\').Where(x => !string.IsNullOrEmpty(x)).Last();
            name = name.LastIndexOf('.') != -1 ? name.Substring(0, name.LastIndexOf('.')) : name;
            string zipName = name + ".zip";
            string zipPath = Path.Combine(conf.DistDirectory, zipName);

            if (conf.IsDirectory(conf.Source))
            {
                CompressFromDirectory(conf, zipPath);
            }
            else if (conf.IsFile(conf.Source))
            {
                CompressFromFile(conf, zipName);
            }
        }

        public static void Uncompress(Config conf)
        {
            if (!conf.IsFile(conf.Source))
                throw new Exception("File not exists.");
            ZipFile.ExtractToDirectory(conf.Source, conf.DistDirectory);
        }

        private static void CompressFromFile(Config conf, string zipName)
        {
            FileInfo file = new FileInfo(conf.Source);
            using (var memoryStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    var demoFile = archive.CreateEntry(file.Name);

                    using (var entryStream = demoFile.Open())
                    using (var fStream = file.OpenRead())
                    {
                        byte[] buffer = new byte[fStream.Length];
                        fStream.Read(buffer, 0, buffer.Length);
                        entryStream.Write(buffer, 0, buffer.Length);
                    }
                }

                using (var fileStream = new FileStream(conf.DistDirectory + zipName, FileMode.Create))
                {
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    memoryStream.CopyTo(fileStream);
                }
            }
        }

        private static void CompressFromDirectory(Config conf, string zipPath)
        {
            if (!Directory.Exists(conf.Source))
                throw new Exception("Directory source not exists");
            string tempzipName = Guid.NewGuid().ToString() + "_zipper.zip";
            string tempZipPath = Path.Combine(Path.GetTempPath(), tempzipName);

            if (File.Exists(tempZipPath))
                File.Delete(tempZipPath);

            ZipFile.CreateFromDirectory(conf.Source, tempZipPath);

            if (File.Exists(zipPath))
                File.Delete(zipPath);

            File.Move(tempZipPath, zipPath);
        }

    }

    class Config
    {
        public const string COMPRESS = "-C";
        public const string UNCOMPRESS = "-U";

        private string _type = COMPRESS;
        private string _source;
        private string _distDir;

        public string Type { get { return _type; } }
        public string Source { get { return _source; } }
        public string DistDirectory { get { return _distDir; } }

        public Config(string[] args)
        {
            if (args.Length > 0)
            {
                SetType(args);
                SetSource(args);
                SetDist(args);
                Validate();
            }
            else
                throw new Exception("Empty params");
        }

        public void SetType(string[] args)
        {
            if (args[0].ToUpper() == COMPRESS || args[0].ToUpper() == UNCOMPRESS)
                this._type = args[0].ToUpper();
        }

        public void SetSource(string[] args)
        {
            bool iterate = true;
            int i = 0;
            while (iterate && args.Length > i)
            {
                if (IsDirectory(args[i]) || IsFile(args[i]))
                {
                    this._source = args[i];
                    iterate = false;
                }
                i++;
            }
        }

        public void SetDist(string[] args)
        {
            bool iterate = true;
            int i = args.Length;
            while (iterate && 0 < i)
            {
                i--;
                if (IsDirectory(args[i]))
                {
                    this._distDir = args[i];
                    iterate = false;
                }
                else if (IsFile(args[i]))
                {
                    this._distDir = args[i].Substring(0, args[i].LastIndexOf('\\') + 1);
                    iterate = false;
                }
            }
        }

        public void Validate()
        {
            if (string.IsNullOrEmpty(this._type))
                throw new Exception("Error Type.");
            if (string.IsNullOrEmpty(this._source))
                throw new Exception("Error Source.");
        }

        public bool IsDirectory(string dir)
        {
            int index = dir.LastIndexOf('\\');
            index = index == -1 ? 0 : index;
            return !dir.Substring(index).Contains(".") && dir.Contains("\\");
        }

        public bool IsFile(string path)
        {
            return File.Exists(path);
        }

        public static void GetHelp()
        {
            Console.WriteLine("--> ZIPPER [-C | -U] Source [DistDirectory]");
            Console.WriteLine("-C - compress file or directory (default)");
            Console.WriteLine("-U - uncompress zip file");
            Console.WriteLine("Source - source file or directory or zip file to compress/uncompress");
            Console.WriteLine("DistDirectory - distenation folder for exit files");
        }
    }
}
