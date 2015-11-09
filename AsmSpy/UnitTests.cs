using System.IO;

namespace AsmSpy
{
    public class UnitTests
    {
        public void AnalyseAssemblies_should_output_correct_info_only_conflicts()
        {
            const string path = @"C:\Dev\Euler\EulerCSharp\bin\Debug";
            Program.AnalyseAssemblies(new DirectoryInfo(path), true, false);
        }

        public void AnalyseAssemblies_should_output_correct_info()
        {
            const string path = @"C:\Dev\Euler\EulerCSharp\bin\Debug";
            Program.AnalyseAssemblies(new DirectoryInfo(path), false, false);
        }
    }
}