// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Chaos;

/// <summary>
/// Applies state-aware container controls without masking unrelated Docker failures.
/// </summary>
/// <param name="run">The Docker command runner.</param>
/// <param name="log">The schedule-log writer.</param>
internal sealed class ContainerControl(Func<IReadOnlyList<string>, CancellationToken, Task<string>> run, Action<string> log)
{
    /// <summary>Projects diagnostic state fields without inspecting environment credentials.</summary>
    const string StateFormat = """{"Status":{{json .State.Status}},"Running":{{json .State.Running}},"Paused":{{json .State.Paused}},"ExitCode":{{json .State.ExitCode}},"Error":{{json .State.Error}},"OOMKilled":{{json .State.OOMKilled}},"FinishedAt":{{json .State.FinishedAt}}}""";

    /// <summary>
    /// Applies a control operation or records that its target state is already satisfied.
    /// </summary>
    /// <param name="container">The dedicated container identifier.</param>
    /// <param name="command">The requested Docker control command.</param>
    /// <param name="cancellation">The existing workload deadline.</param>
    /// <returns>The command output, or an empty string when no transition is needed.</returns>
    public async Task<string> Apply(string container, string command, CancellationToken cancellation)
    {
        var state = await ReadState(container, cancellation);
        var plan = state.Classify(command);
        Report(container, command, state, plan.Outcome);
        if (plan.Command is null) return string.Empty;
        try
        {
            return await run(Arguments(container, plan.Command), cancellation);
        }
        catch (InvalidOperationException)
        {
            // A process may exit between inspect and control. Only a freshly satisfied state is benign.
            var current = await ReadState(container, cancellation);
            var outcome = current.Classify(command);
            if (outcome.Command is not null) throw;
            Report(container, command, current, outcome.Outcome);
            return string.Empty;
        }
    }

    static IReadOnlyList<string> Arguments(string container, string command) => command == "kill"
        ? [command, "--signal", "KILL", container]
        : [command, container];

    async Task<ContainerFaultState> ReadState(string container, CancellationToken cancellation)
    {
        var json = await run(["inspect", "--format", StateFormat, container], cancellation);
        return JsonSerializer.Deserialize<ContainerFaultState>(json) ?? throw new InvalidOperationException("Docker returned no container state.");
    }

    void Report(string container, string command, ContainerFaultState state, string outcome) =>
        log($"FAULT outcome=\"{outcome}\" container={container} command={command} state={JsonSerializer.Serialize(state)}");
}
