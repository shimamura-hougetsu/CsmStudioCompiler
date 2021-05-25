using CsmStudioCompiler;
using CommandLine;


namespace CsmStudioCompilerCli
{
    class Program
    {
        static int Main(string[] args)
        {
            return CommandLine.Parser.Default.ParseArguments<CompilingOptions>(args).MapResult(
                opts => CompilerInvoker.CompileProjectWithOptions(opts),
                _ => -1
                );
        }
    }
}
