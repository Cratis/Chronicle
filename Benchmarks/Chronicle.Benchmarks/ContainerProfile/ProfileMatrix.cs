// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.ContainerProfile;

/// <summary>The exhaustive and focused resource matrices.</summary>
internal static class ProfileMatrix
{
    /// <summary>Gets every cell for the given CPU quotas and memory limits.</summary>
    /// <param name="cpus">The CPU quotas.</param>
    /// <param name="memory">The memory limits, in Docker notation.</param>
    /// <returns>Each combination of quota, limit, GC variant and image variant.</returns>
    public static IEnumerable<MatrixCell> Full(IEnumerable<int> cpus, IEnumerable<string> memory) =>
        from cpu in cpus
        from limit in memory
        from gc in GcVariant.All
        from image in new[] { ImageVariant.R2R, ImageVariant.Jit }
        select new MatrixCell(cpu, limit, gc, image);

    /// <summary>Gets the R2R cells and the focused JIT comparison cells at 1 GiB.</summary>
    /// <param name="cpus">The CPU quotas.</param>
    /// <param name="memory">The memory limits, in Docker notation.</param>
    /// <returns>The focused cells.</returns>
    public static IEnumerable<MatrixCell> Core(IEnumerable<int> cpus, IEnumerable<string> memory) => Full(cpus, memory).Where(cell =>
        cell.Image == ImageVariant.R2R || (cell.Memory == "1g" && (cell.Gc.Name == "server" || cell.Gc.Name == "workstation")));

    /// <summary>Selects cells using comma-separated substring filters.</summary>
    /// <param name="options">The selection options.</param>
    /// <returns>The matching cells.</returns>
    public static MatrixCell[] Select(ProfileOptions options) =>
        (options.Matrix == "core" ? Core(options.Cpus, options.Memory) : Full(options.Cpus, options.Memory))
            .Where(cell => options.Cells.Length == 0 || options.Cells.Any(filter => cell.Id.Contains(filter, StringComparison.Ordinal)))
            .ToArray();
}
