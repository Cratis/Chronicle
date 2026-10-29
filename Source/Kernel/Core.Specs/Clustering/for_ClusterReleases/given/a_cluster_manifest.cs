// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Net;
using Cratis.Chronicle.Concepts.System;
using Cratis.Chronicle.Observation;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Metadata;
using Orleans.Serialization;
using Orleans.Serialization.TypeSystem;

namespace Cratis.Chronicle.Clustering.for_ClusterReleases.given;

public class a_cluster_manifest : Specification
{
    protected readonly SemanticVersion _current = new(19, 112, 0);
    protected readonly IClusterManifestProvider _provider = Substitute.For<IClusterManifestProvider>();
    protected readonly Dictionary<SiloAddress, GrainManifest> _silos = [];
    protected ClusterReleases _releases;
    protected SiloAddress _local;
    protected SiloAddress _other;
    protected GrainInterfaceType _observer;
    ServiceProvider _services;
    protected GrainInterfaceTypeResolver _resolver;
    long _version;

    void Establish()
    {
        _services = new ServiceCollection().AddSerializer().BuildServiceProvider();
        _resolver = new([], _services.GetRequiredService<TypeConverter>());
        _observer = _resolver.GetGrainInterfaceType(typeof(IObserver));
        _local = Add("19.112.0");
        _other = Add("19.111.0");
        var local = Substitute.For<ILocalSiloDetails>();
        local.SiloAddress.Returns(_local);
        _releases = new(_provider, _resolver, _current);
    }

    void Destroy() => _services.Dispose();

    protected SiloAddress Add(string? release)
    {
        var silo = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11111 + _silos.Count), 1);
        Set(silo, release);
        return silo;
    }

    protected void Set(SiloAddress silo, string? release)
    {
        var properties = ImmutableDictionary.Create<string, string>(StringComparer.Ordinal);
        if (release is not null) properties = properties.Add(KernelReleaseInterfaceProperties.ReleaseKey, release);
        _silos[silo] = new([], ImmutableDictionary<GrainInterfaceType, GrainInterfaceProperties>.Empty.Add(_observer, new(properties)));
        Publish();
    }

    protected void RemoveObserver(SiloAddress silo)
    {
        _silos[silo] = new([], []);
        Publish();
    }

    protected ClusterReleases ForVersion(SemanticVersion version)
    {
        return new(_provider, _resolver, version);
    }

    protected void Publish() => _provider.Current.Returns(new ClusterManifest(new MajorMinorVersion(++_version, 0), _silos.ToImmutableDictionary()));
}
