// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.ContainerProfile;

/// <summary>The exhaustive and focused resource matrices.</summary>
internal static class ProfileMatrix
{
    static readonly int[] _cpus = [1, 2];
    static readonly string[] _memory = ["512m", "1g", "2g"];

    /// <summary>Gets all 72 cells.</summary>
    public static IEnumerable<MatrixCell> Full =>
        from cpu in _cpus
        from memory in _memory
        from gc in GcVariant.All
        from image in new[] { ImageVariant.R2R, ImageVariant.Jit }
        select new MatrixCell(cpu, memory, gc, image);

    /// <summary>Gets 36 R2R cells and four focused JIT comparison cells.</summary>
    public static IEnumerable<MatrixCell> Core => Full.Where(cell =>
        cell.Image == ImageVariant.R2R || (cell.Memory == "1g" && (cell.Gc.Name == "server" || cell.Gc.Name == "workstation")));

    /// <summary>Selects cells using comma-separated substring filters.</summary>
    /// <param name="options">The selection options.</param>
    /// <returns>The matching cells.</returns>
    public static MatrixCell[] Select(ProfileOptions options) =>
        (options.Matrix == "core" ? Core : Full)
            .Where(cell => options.Cells.Length == 0 || options.Cells.Any(filter => cell.Id.Contains(filter, StringComparison.Ordinal)))
            .ToArray();
}
