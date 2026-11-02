// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Benchmarks.ContainerProfile;

/// <summary>A deterministic, process-free validation and command preview.</summary>
internal static class ProfilePlan
{
    /// <summary>Validates and renders the exact command builders without invoking them.</summary>
    /// <param name="options">The matrix and workload options.</param>
    /// <returns>Zero only for a nonempty, valid plan.</returns>
    public static int Print(ProfileOptions options)
    {
        var cells = ProfileMatrix.Select(options);
        var violations = new List<string>();
        if (cells.Length == 0)
        {
            violations.Add("Cell filter matched no cells.");
        }
        foreach (var cell in cells)
        {
            violations.AddRange(InspectCell(cell, options, cell == cells[0]));
        }
        var runs = cells.Length * (options.Repetitions + (options.GcTrace ? 1 : 0));
        Console.WriteLine($"Workload: {JsonSerializer.Serialize(options.Workload)}; client and MongoDB unconstrained; Workbench assets absent.");
        Console.WriteLine($"Cells: {cells.Length}; repetitions: {options.Repetitions}; trace repetitions per cell: {(options.GcTrace ? 1 : 0)}; runs: {runs}");
        Console.WriteLine($"Estimated wall time: {runs * 15} minutes ({runs * 0.25:F2} hours), budgeting 15 min/run; image builds excluded. No processes started.");
        foreach (var violation in violations)
        {
            Console.Error.WriteLine(violation);
        }
        Console.WriteLine($"Invariant violations: {violations.Count}");
        return violations.Count == 0 ? 0 : 1;
    }

    static IEnumerable<string> InspectCell(MatrixCell cell, ProfileOptions options, bool representative)
    {
        Console.WriteLine(cell.Id);
        foreach (var trace in options.GcTrace ? (bool[])[false, true] : [false])
        {
            var session = new ProfileSession("plan", "/plan", "<shortsha>", "mongo:8.2");
            var context = new RunContext(session, new RunIdentity("run", 1, trace), "/plan", new RunCredentials("<generated>", "<generated>"));
            var commands = Commands(context, cell).ToArray();
            var violations = commands.SelectMany(command => CommandValidation.Validate(command, cell, context.Session)).ToArray();
            if (options.Verbose || representative)
            {
                PrintCommands(commands);
            }
            foreach (var violation in violations)
            {
                yield return violation;
            }
        }
    }

    static void PrintCommands(IEnumerable<DockerCommand> commands)
    {
        foreach (var command in commands)
        {
            Console.WriteLine($"  {command.Purpose}: {command.Display}");
        }
    }

    static IEnumerable<DockerCommand> Commands(RunContext context, MatrixCell cell) =>
    [
        MetricCommands.Architecture(), MetricCommands.Info(), MetricCommands.Image(cell.Image.Reference(context.Sha)),
        MetricCommands.Image(context.MongoImage), ImageCommands.Pull(context.MongoImage), ImageCommands.Pull("busybox:1.36"),
        ContainerCommands.Network(context), ContainerCommands.Mongo(context), ContainerCommands.Start(context.Mongo),
        ContainerCommands.Initiate(context), ContainerCommands.Primary(context), ContainerCommands.Kernel(context, cell),
        ContainerCommands.Start(context.Kernel), MetricCommands.Port(context, 8080), MetricCommands.Port(context, 35000),
        MetricCommands.Status(context), MetricCommands.Stats(context), MetricCommands.State(context),
        MetricCommands.Stop(context), MetricCommands.State(context), CleanupCommands.Remove(context.Kernel, context.Mongo),
        CleanupCommands.RemoveNetwork(context.Network), CleanupCommands.Containers(), CleanupCommands.Networks()
    ];
}
