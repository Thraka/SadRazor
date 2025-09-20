using SadRazor.Cli.Commands;
using System.CommandLine;

namespace SadRazor.Cli;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("SadRazor CLI - A Razor-based Markdown templating engine")
        {
            Name = "sadrazor"
        };

        // Add commands
        rootCommand.AddCommand(new RenderCommand());
        rootCommand.AddCommand(new BatchCommand());
        rootCommand.AddCommand(new WatchCommand());
        rootCommand.AddCommand(new InitCommand());
        rootCommand.AddCommand(new ValidateCommand());

        return await rootCommand.InvokeAsync(args);
    }
}