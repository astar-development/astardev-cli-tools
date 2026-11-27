using AStarDev.BranchGap.Cli;

return await BranchGapCommand.Build(Console.Out, Console.Error).Parse(args).InvokeAsync();
