// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.ContainerProfile;

/// <summary>One explicit GC configuration delta from the shipped Server GC default.</summary>
/// <param name="Name">The stable variant name.</param>
/// <param name="Environment">The runtime environment overrides.</param>
internal sealed record GcVariant(string Name, IReadOnlyList<string> Environment)
{
    /// <summary>The six independent GC variants.</summary>
    public static readonly GcVariant[] All =
    [
        new("server", []),
        new("server-datas-on", ["DOTNET_GCDynamicAdaptationMode=1"]),
        new("server-datas-off", ["DOTNET_GCDynamicAdaptationMode=0"]),
        new("server-conserve-0", ["DOTNET_GCConserveMemory=0"]),
        new("server-hardlimit-75", ["DOTNET_GCHeapHardLimitPercent=0x4B"]),
        new("workstation", ["DOTNET_gcServer=0"])
    ];
}
