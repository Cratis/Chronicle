// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Cli;

namespace Cratis.Chronicle.Integration.Cli;

/// <summary>
/// Runs Chronicle CLI commands programmatically and captures their output for integration testing.
/// </summary>
public static class CliCommandRunner
{
    /// <summary>
    /// Executes the CLI with the given arguments, capturing stdout and stderr.
    /// </summary>
    /// <param name="args">The command-line arguments to pass to the CLI.</param>
    /// <returns>A <see cref="CliCommandResult"/> containing the exit code and captured output.</returns>
    public static async Task<CliCommandResult> RunAsync(params string[] args)
    {
        // StringWriter wraps a StringBuilder and has no unmanaged resources to release.
        // We intentionally avoid 'using'/'await using' because AnsiConsole.Console is a static
        // singleton that captures Console.Out at first access. Disposing the writer while the
        // static reference is alive causes ObjectDisposedException in subsequent test runs.
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var originalOut = Console.Out;
        var originalErr = Console.Error;

        try
        {
            Console.SetOut(stdout);
            Console.SetError(stderr);

            var app = CliApp.Create();
            var exitCode = await app.RunAsync(args);

            return new CliCommandResult(exitCode, stdout.ToString(), stderr.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalErr);
        }
    }
}
