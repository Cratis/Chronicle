// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Sys;
using Orleans.Metadata;

namespace Cratis.Chronicle.Releases;

/// <summary>
/// Reads kernel releases from the Orleans cluster manifest, caching only until its version changes.
/// </summary>
public sealed class ClusterReleases : IClusterReleases
{
    readonly IClusterManifestProvider _manifests;
    readonly GrainInterfaceType _observer;
    readonly SemanticVersion _current;
    Snapshot? _snapshot;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClusterReleases"/> class.
    /// </summary>
    /// <param name="manifests">The cluster manifest provider.</param>
    /// <param name="resolver">The grain interface type resolver.</param>
    /// <param name="localSiloDetails">The local silo details.</param>
    public ClusterReleases(IClusterManifestProvider manifests, GrainInterfaceTypeResolver resolver, ILocalSiloDetails localSiloDetails)
        : this(manifests, resolver, KernelVersion.Current)
    {
        ArgumentNullException.ThrowIfNull(localSiloDetails);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ClusterReleases"/> class for an explicit local release.
    /// </summary>
    /// <param name="manifests">The cluster manifest provider.</param>
    /// <param name="resolver">The grain interface resolver.</param>
    /// <param name="current">The local kernel release.</param>
    internal ClusterReleases(IClusterManifestProvider manifests, GrainInterfaceTypeResolver resolver, SemanticVersion current)
    {
        _manifests = manifests;
        _observer = resolver.GetGrainInterfaceType(typeof(IObserver));
        _current = current;
    }

    /// <inheritdoc/>
    public IReadOnlyList<SiloAddress> AtLeastAsNewAsThisSilo(IEnumerable<SiloAddress> candidates)
    {
        if (!KernelVersion.IsKnown(_current)) return candidates.ToArray();
        var releases = Read().Releases;
        return candidates.Where(silo => releases.TryGetValue(silo, out var release) && release is not null && release >= _current).ToArray();
    }

    /// <inheritdoc/>
    public ClusterReleaseSummary Summarize() => Read().Summary;

    static bool IsReleaseCharacter(char character) => char.IsAsciiLetterOrDigit(character) || character is '.' or '-' or '+';

    Snapshot Read()
    {
        var manifest = _manifests.Current;
        var snapshot = Volatile.Read(ref _snapshot);
        if (snapshot is not null && snapshot.Version == manifest.Version) return snapshot;
        var releases = manifest.Silos.ToDictionary(_ => _.Key, _ => Parse(_.Value));
        var groups = releases.Values.Where(_ => _ is not null).Cast<SemanticVersion>()
            .GroupBy(_ => _).OrderBy(_ => _.Key).Select(_ => (_.Key, _.Count())).ToArray();
        snapshot = new(manifest.Version, releases, new(groups.Take(8).ToArray(), releases.Values.Count(_ => _ is null), Math.Max(0, groups.Length - 8)));
        Volatile.Write(ref _snapshot, snapshot);
        return snapshot;
    }

    SemanticVersion? Parse(GrainManifest manifest)
    {
        if (!manifest.Interfaces.TryGetValue(_observer, out var properties)) return null;
        if (!properties.Properties.TryGetValue(KernelReleaseInterfaceProperties.ReleaseKey, out var value)) return null;
        if (value.Length > 64 || !value.All(IsReleaseCharacter)) return null;
        return SemanticVersion.TryParse(value, out var release) ? release : null;
    }

    sealed record Snapshot(MajorMinorVersion Version, IReadOnlyDictionary<SiloAddress, SemanticVersion?> Releases, ClusterReleaseSummary Summary);
}
