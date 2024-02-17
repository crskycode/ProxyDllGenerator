using PeNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ProxyDllGenerator
{
    internal class Program
    {
        static List<string> GetExports(string path)
        {
            var pe = new PeFile(path);
            return pe.ExportedFunctions.Select(x => x.Name).ToList();
        }

        static void CreateForwardCpp(string path, string lib, List<string> names)
        {
            var tpl = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "Forward.cpp.txt");

            var sw = File.CreateText(path);
            var str = File.ReadAllText(tpl);

            var sb = new StringBuilder();

            for (var i = 0; i < names.Count; i++)
            {
                sb.AppendFormat("\tLPVOID pfn{0} = NULL;", names[i]);
                if (i < names.Count - 1)
                    sb.AppendLine();
            }

            str = str.Replace("%0", sb.ToString());
            str = str.Replace("%1", lib);

            sb.Clear();

            for (var i = 0; i < names.Count; i++)
            {
                sb.AppendFormat("\tpfn{0} = GetProcAddress(g_Dll, \"{0}\");", names[i]);
                if (i < names.Count - 1)
                    sb.AppendLine();
            }

            str = str.Replace("%2", sb.ToString());

            sw.Write(str);

            sw.Flush();
            sw.Dispose();
        }

        static void CreateForwardHpp(string path, List<string> names)
        {
            var tpl = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "Forward.h.txt");

            var sw = File.CreateText(path);
            var str = File.ReadAllText(tpl);

            var sb = new StringBuilder();

            for (var i = 0; i < names.Count; i++)
            {
                sb.AppendFormat("\textern LPVOID pfn{0};", names[i]);
                if (i < names.Count - 1)
                    sb.AppendLine();
            }

            str = str.Replace("%0", sb.ToString());

            sw.Write(str);

            sw.Flush();
            sw.Dispose();
        }

        static void CreateForwardDef(string path, List<string> names)
        {
            var sw = File.CreateText(path);

            sw.WriteLine("EXPORTS");

            for (var i = 0; i < names.Count; i++)
            {
                sw.WriteLine("{0}=fw{0}", names[i]);
            }

            sw.Flush();
            sw.Dispose();
        }

        static void CreateForward32Asm(string path, List<string> names)
        {
            var sw = File.CreateText(path);

            sw.WriteLine(".386");
            sw.WriteLine(".MODEL FLAT");
            sw.WriteLine();

            for (var i = 0; i < names.Count; i++)
            {
                sw.WriteLine("EXTERN C pfn{0} : DWORD", names[i]);
            }

            sw.WriteLine();
            sw.WriteLine(".CODE");
            sw.WriteLine();

            for (var i = 0; i < names.Count; i++)
            {
                sw.WriteLine("fw{0} PROC", names[i]);
                sw.WriteLine("\tJMP DWORD PTR [pfn{0}]", names[i]);
                sw.WriteLine("fw{0} ENDP", names[i]);
                sw.WriteLine();
            }

            sw.WriteLine("END");

            sw.Flush();
            sw.Dispose();
        }

        static void CreateForward64Asm(string path, List<string> names)
        {
            var sw = File.CreateText(path);

            for (var i = 0; i < names.Count; i++)
            {
                sw.WriteLine("EXTERN pfn{0} : QWORD", names[i]);
            }

            sw.WriteLine();
            sw.WriteLine(".CODE");
            sw.WriteLine();

            for (var i = 0; i < names.Count; i++)
            {
                sw.WriteLine("fw{0} PROC", names[i]);
                sw.WriteLine("\tJMP QWORD PTR [pfn{0}]", names[i]);
                sw.WriteLine("fw{0} ENDP", names[i]);
                sw.WriteLine();
            }

            sw.WriteLine("END");

            sw.Flush();
            sw.Dispose();
        }

        static void Main(string[] args)
        {
            var outDirPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Generated");

            var lib = "version.dll";
            var names = GetExports(@"C:\Windows\System32\version.dll");

            Directory.CreateDirectory(outDirPath);

            CreateForwardCpp(Path.Combine(outDirPath, "Forward.cpp"), lib, names);
            CreateForwardHpp(Path.Combine(outDirPath, "Forward.h"), names);
            CreateForwardDef(Path.Combine(outDirPath, "Forward.def"), names);
            CreateForward32Asm(Path.Combine(outDirPath, "Forward32.asm"), names);
            CreateForward64Asm(Path.Combine(outDirPath, "Forward64.asm"), names);
        }
    }
}
