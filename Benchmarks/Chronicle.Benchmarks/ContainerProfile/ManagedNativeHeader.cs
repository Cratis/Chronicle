// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection.PortableExecutable;

namespace Cratis.Chronicle.Benchmarks.ContainerProfile;

/// <summary>The PE-level ReadyToRun observation for a Chronicle assembly.</summary>
/// <param name="Assembly">The assembly filename.</param>
/// <param name="ReadyToRun">Whether its managed native header is present.</param>
internal sealed record ManagedNativeHeader(string Assembly, bool ReadyToRun)
{
    /// <summary>Reads all Chronicle PE headers in a publish directory.</summary>
    /// <param name="directory">The application directory.</param>
    /// <returns>The assembly observations.</returns>
    public static ManagedNativeHeader[] Read(string directory) =>
        Directory.GetFiles(directory, "Cratis.Chronicle*.dll", SearchOption.TopDirectoryOnly)
            .Select(ReadAssembly).ToArray();

    /// <summary>Requires a server assembly, a nonempty population and consistent headers.</summary>
    /// <param name="headers">The observed assemblies.</param>
    /// <param name="expected">The expected ReadyToRun state.</param>
    /// <returns>Whether the observations match.</returns>
    public static bool Matches(ManagedNativeHeader[] headers, bool expected) =>
        headers.Any(header => header.Assembly == "Cratis.Chronicle.Server.dll") && headers.All(header => header.ReadyToRun == expected);

    static ManagedNativeHeader ReadAssembly(string path)
    {
        using var stream = File.OpenRead(path);
        using var pe = new PEReader(stream);
        var native = pe.PEHeaders.CorHeader?.ManagedNativeHeaderDirectory;
        return new ManagedNativeHeader(Path.GetFileName(path), native is { Size: > 0, RelativeVirtualAddress: > 0 });
    }
}
