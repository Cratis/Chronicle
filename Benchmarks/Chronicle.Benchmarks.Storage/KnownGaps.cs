// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>
/// The benchmark cases the harness knows it cannot measure honestly yet. They are excluded from the run (so they can
/// neither fail it nor report a made-up number) and listed in the summary as not measured.
/// </summary>
public static class KnownGaps
{
    /// <summary>The reason the SQL activation state read is not measured.</summary>
    public const string SqlActivationState = "not measured — needs namespace storage in the fixture (follow-up)";

    /// <summary>Gets the benchmark method that reads the saved sequence state on activation.</summary>
    public const string GetStateCase = "activation-get-state";

    /// <summary>Gets why a benchmark method is not measured on a provider.</summary>
    /// <param name="provider">The provider.</param>
    /// <param name="benchmarkMethod">The benchmark method name, for example `GetState`.</param>
    /// <returns>The reason, or null when it is measured.</returns>
    public static string? ReasonFor(StorageProvider provider, string benchmarkMethod) =>
        benchmarkMethod == nameof(ActivationStorageBenchmark.GetState) && provider != StorageProvider.Mongo ? SqlActivationState : null;

    /// <summary>Gets the cases that are not measured on a provider.</summary>
    /// <param name="provider">The provider.</param>
    /// <returns>The case names and reasons.</returns>
    public static IReadOnlyList<(string Case, string Reason)> For(StorageProvider provider) =>
        ReasonFor(provider, nameof(ActivationStorageBenchmark.GetState)) is { } reason ? [(GetStateCase, reason)] : [];
}
