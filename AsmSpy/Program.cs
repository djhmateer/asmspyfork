using AsmSpy.Native;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AsmSpy
{
    public class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 3 || args.Length < 1)
            {
                PrintUsage();
                return;
            }

            var directoryPath = args[0];
            if (!Directory.Exists(directoryPath))
            {
                PrintDirectoryNotFound(directoryPath);
                return;
            }

            // eg "all", "all nonsystem", "nonsystem"
            bool onlyConflicts = !args.Skip(1).Any(x => x.Equals("all"));
            bool skipSystem = args.Skip(1).Any(x => x.Equals("nonsystem"));

            AnalyseAssemblies(new DirectoryInfo(directoryPath), onlyConflicts, skipSystem);
        }

        public static void AnalyseAssemblies(DirectoryInfo directoryInfo, bool onlyConflicts, bool skipSystem)
        {
            var assemblyFiles = directoryInfo.GetFiles("*.dll").Concat(directoryInfo.GetFiles("*.exe")).ToList();
            if (!assemblyFiles.Any())
            {
                Console.WriteLine("No dll files found in directory: '{0}'", directoryInfo.FullName);
                return;
            }

            Console.WriteLine("Checking assemblies in:");
            Console.WriteLine(directoryInfo.FullName);
            Console.WriteLine();

            // Creating a Dictionary of assembly (each file is 1 assembly) names, and a List of it's referenced Assemblies
            var assemblies = new Dictionary<string, IList<ReferencedAssembly>>();
            foreach (var fileInfo in assemblyFiles.OrderBy(asm => asm.Name))
            {
                Assembly assembly;
                try
                {
                    // FileInfo extension method in Native folder.  Under what scenarios needed? Why?
                    if (!fileInfo.IsAssembly()) continue;
                    assembly = Assembly.ReflectionOnlyLoadFrom(fileInfo.FullName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to load assembly '{0}': {1}", fileInfo.FullName, ex.Message);
                    continue;
                }

                foreach (var referencedAssembly in assembly.GetReferencedAssemblies())
                {
                    if (!assemblies.ContainsKey(referencedAssembly.Name))
                        assemblies.Add(referencedAssembly.Name, new List<ReferencedAssembly>());

                    assemblies[referencedAssembly.Name]
                        .Add(new ReferencedAssembly(referencedAssembly.Version, assembly));
                }
            }

            if (onlyConflicts)
                Console.WriteLine("Detailing only conflicting assembly references.");

            foreach (var assembly in assemblies)
            {
                if (skipSystem && (assembly.Key.StartsWith("System") || assembly.Key.StartsWith("mscorlib"))) continue;

                // like a sql statement - if not onlyConflicts then ok
                if (!onlyConflicts
                    //|| (onlyConflicts && assembly.Value.GroupBy(x => x.VersionReferenced).Count() != 1))
                    || assembly.Value.GroupBy(x => x.VersionReferenced).Count() != 1)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write("Reference: ");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine("{0}", assembly.Key);

                    // the output
                    var referencedAssemblies = new List<Tuple<string, string>>();
                    var versionsList = new List<string>();
                    var asmList = new List<string>();
                    foreach (var referencedAssembly in assembly.Value)
                    {
                        var s1 = referencedAssembly.VersionReferenced.ToString();
                        var s2 = referencedAssembly.ReferencedBy.GetName().Name;
                        var tuple = new Tuple<string, string>(s1, s2);
                        referencedAssemblies.Add(tuple);
                    }
                    
                    foreach (var referencedAssembly in referencedAssemblies)
                    {
                        if (!versionsList.Contains(referencedAssembly.Item1))
                            versionsList.Add(referencedAssembly.Item1);
                        if (!asmList.Contains(referencedAssembly.Item1))
                            asmList.Add(referencedAssembly.Item1);
                    }

                    // ouptut to console
                    foreach (var referencedAssembly in referencedAssemblies)
                    {
                        var versionColor = ConsoleColors[versionsList.IndexOf(referencedAssembly.Item1) % ConsoleColors.Length];

                        Console.ForegroundColor = versionColor;
                        Console.Write("   {0}", referencedAssembly.Item1);
                        
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.Write(" by ");
                        
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.WriteLine("{0}", referencedAssembly.Item2);
                    }

                    Console.WriteLine();
                }
            }
        }

        // create array of ConsoleColor's.. only used above to get versionColor
        static readonly ConsoleColor[] ConsoleColors = {
                ConsoleColor.Green,
                ConsoleColor.Red,
                ConsoleColor.Yellow,
                ConsoleColor.Blue,
                ConsoleColor.Cyan,
                ConsoleColor.Magenta,
            };

        private static void PrintDirectoryNotFound(string directoryPath)
        {
            Console.WriteLine("Directory: '" + directoryPath + "' does not exist.");
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("AsmSpy <directory to load assemblies from> [all]");
            Console.WriteLine("E.g.");
            Console.WriteLine(@"AsmSpy C:\Source\My.Solution\My.Project\bin\Debug");
            Console.WriteLine(@"AsmSpy C:\Source\My.Solution\My.Project\bin\Debug all");
        }
    }

    public class ReferencedAssembly
    {
        public Version VersionReferenced { get; }
        public Assembly ReferencedBy { get; }

        public ReferencedAssembly(Version versionReferenced, Assembly referencedBy)
        {
            VersionReferenced = versionReferenced;
            ReferencedBy = referencedBy;
        }
    }
}
