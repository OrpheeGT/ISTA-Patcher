// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2026 TautCony

using System.Runtime.InteropServices;
using DotMake.CommandLine;
using ISTAPatcher;
using ISTAPatcher.Commands;
using ISTAPatcher.Tasks;
using Serilog;

TaskProvider.GatherTasks<IStartupTask>().Run(args);
using var termination = new CancellationTokenSource();
using var sigTermRegistration = PosixSignalRegistration.Create(PosixSignal.SIGTERM, context =>
{
    context.Cancel = true;
    RequestTermination("SIGTERM");
});

Global.CancellationToken = termination.Token;

var cancellationRequested = 0;
Console.CancelKeyPress += (_, eventArgs) =>
{
    if (Interlocked.Exchange(ref cancellationRequested, 1) == 0)
    {
        eventArgs.Cancel = true;
        RequestTermination("Ctrl+C");
        return;
    }

    Log.Warning("Termination requested again, exiting immediately.");
};

var theme = new CliTheme(CliTheme.Default)
{
    DefaultStyle = new CliStyle(ConsoleColor.DarkGray),
    HeadingStyle = new CliStyle(ConsoleColor.Blue),
    FirstColumnStyle = new CliStyle(ConsoleColor.Cyan),
    SecondColumnStyle = new CliStyle(ConsoleColor.Green),
};
try
{
    return await Cli.RunAsync<RootCommand>(args, new CliSettings { Theme = theme });
}
catch (OperationCanceledException) when (termination.IsCancellationRequested)
{
    Log.Warning("Operation cancelled by termination request.");
    return 130;
}

void RequestTermination(string reason)
{
    if (termination.IsCancellationRequested)
    {
        return;
    }

    Log.Warning("Termination requested ({Reason}). Cancelling current operation...", reason);
    termination.Cancel();
}
