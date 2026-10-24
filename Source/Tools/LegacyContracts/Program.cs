// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Tools.LegacyContracts;

try
{
    var options = Options.Parse(args);
    if (options.SelfTest)
    {
        return SelfTest.Run(Console.Out) == 0 ? 0 : 1;
    }

    var manifest = await File.ReadAllTextAsync(options.Manifest!);
    var issues = await File.ReadAllTextAsync(options.Issues!);
    var run = options.Structural
        ? EvaluationRun.ReadStructural(manifest, issues, options.Mode)
        : EvaluationRun.Read(manifest, issues, await File.ReadAllTextAsync(options.Jobs!), () => TestResults.Read(options.Trx!), options.Mode);
    var report = ReportRenderer.Text(run);
    await Console.Out.WriteAsync(report);
    if (options.GitHub)
    {
        await Console.Out.WriteLineAsync(ReportRenderer.WorkflowCommands(run));
        var summary = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
        if (!string.IsNullOrWhiteSpace(summary))
        {
            await File.AppendAllTextAsync(summary, report);
        }
    }

    return run.ExitCode;
}
catch (Exception ex) when (ex is CannotEvaluate or IOException or UnauthorizedAccessException)
{
    await Console.Error.WriteLineAsync($"Could not evaluate: {ex.Message}");
    return 2;
}
