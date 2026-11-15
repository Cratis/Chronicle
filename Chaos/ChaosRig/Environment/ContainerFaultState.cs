// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Chaos;

/// <summary>
/// Contains only Docker state fields needed to classify faults and report exit diagnostics.
/// </summary>
internal sealed record ContainerFaultState
{
    /// <summary>Gets the Docker lifecycle status.</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>Gets whether the container is running.</summary>
    public bool Running { get; init; }

    /// <summary>Gets whether the container is paused.</summary>
    public bool Paused { get; init; }

    /// <summary>Gets the last process exit code.</summary>
    public int ExitCode { get; init; }

    /// <summary>Gets the Docker-reported exit or startup error.</summary>
    public string Error { get; init; } = string.Empty;

    /// <summary>Gets whether Docker reports an out-of-memory kill.</summary>
    public bool OOMKilled { get; init; }

    /// <summary>Gets the last process completion timestamp.</summary>
    public string FinishedAt { get; init; } = string.Empty;

    bool IsDown => !Running && (string.Equals(Status, "created", StringComparison.Ordinal) ||
        string.Equals(Status, "exited", StringComparison.Ordinal) || string.Equals(Status, "dead", StringComparison.Ordinal));

    /// <summary>
    /// Determines whether a fault is already satisfied or needs a Docker transition.
    /// </summary>
    /// <param name="command">The requested Docker control command.</param>
    /// <returns>The required command, if any, and the schedule-log outcome.</returns>
    public (string? Command, string Outcome) Classify(string command) => command switch
    {
        "kill" or "pause" or "unpause" when IsDown => (null, "target already down"),
        "pause" when Paused => (null, "target already paused"),
        "unpause" when Running && !Paused => (null, "target not paused"),
        "start" when IsDown => ("start", "target already down"),
        "start" when Paused => ("unpause", "target paused"),
        "start" when Running => (null, "target already running"),
        _ => (command, "apply")
    };
}
