// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.ContainerProfile;

/// <summary>The scalable workload shape; event counts are per store/namespace pair.</summary>
internal sealed record WorkloadSettings
{
    static readonly Dictionary<string, Func<WorkloadSettings, int, WorkloadSettings>> _options = new(StringComparer.Ordinal)
    {
        ["--stores"] = (settings, value) => settings with { Stores = value },
        ["--namespaces"] = (settings, value) => settings with { Namespaces = value },
        ["--seed-events"] = (settings, value) => settings with { SeedEvents = value },
        ["--events"] = (settings, value) => settings with { Events = value },
        ["--batch-size"] = (settings, value) => settings with { BatchSize = value },
        ["--appenders"] = (settings, value) => settings with { Appenders = value }
    };

    /// <summary>Gets the store count.</summary>
    public int Stores { get; init; } = 3;

    /// <summary>Gets namespaces per store.</summary>
    public int Namespaces { get; init; } = 5;

    /// <summary>Gets seed events per pair.</summary>
    public int SeedEvents { get; init; } = 200;

    /// <summary>Gets measured events per pair.</summary>
    public int Events { get; init; } = 30000;

    /// <summary>Gets events per append.</summary>
    public int BatchSize { get; init; } = 100;

    /// <summary>Gets the global append concurrency.</summary>
    public int Appenders { get; init; } = 4;

    /// <summary>Applies one already-validated workload scale option.</summary>
    /// <param name="key">The CLI option name.</param>
    /// <param name="value">The positive option value.</param>
    /// <returns>The updated scale, or null for an unknown option.</returns>
    public WorkloadSettings? WithOption(string key, int value) =>
        _options.TryGetValue(key, out var apply) ? apply(this, value) : null;
}
