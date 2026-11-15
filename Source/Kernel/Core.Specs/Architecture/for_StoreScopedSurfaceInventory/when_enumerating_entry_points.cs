// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Architecture.for_StoreScopedSurfaceInventory;

public class when_enumerating_entry_points : Specification
{
    IReadOnlyDictionary<string, Classification> _classifications;
    KeyValuePair<string, MethodInfo>[] _surfaces;

    void Establish() => _classifications = SurfaceInventory.Classifications;

    void Because() => _surfaces = SurfaceDiscovery.Discover().ToArray();

    [Fact] void should_discover_grpc_methods() => _surfaces.Where(surface => surface.Key.StartsWith("gRPC:", StringComparison.Ordinal)).ShouldNotBeEmpty();

    [Fact] void should_discover_arc_methods() => _surfaces.Where(surface => surface.Key.StartsWith("Arc:", StringComparison.Ordinal)).ShouldNotBeEmpty();

    [Fact]
    void should_classify_every_discovered_method()
    {
        var unclassified = _surfaces.Select(surface => surface.Key).Except(_classifications.Keys).Order().ToArray();
        string.Join(Environment.NewLine, unclassified).ShouldEqual(string.Empty);
    }

    [Fact]
    void should_have_no_stale_classifications() => _classifications.Keys
        .Except(_surfaces.Select(surface => surface.Key))
        .Order()
        .ShouldBeEmpty();

    [Fact]
    void should_identify_every_method_unambiguously() => _surfaces
        .GroupBy(surface => surface.Key)
        .Where(group => group.Count() > 1)
        .Select(group => $"{group.Key}: {string.Join("; ", group.Select(surface => surface.Value.ToString()))}")
        .ShouldBeEmpty();

    [Fact]
    void should_carry_a_store_argument_or_document_the_exception() => _surfaces
        .Where(surface => _classifications.TryGetValue(surface.Key, out var classification) &&
            classification.IsStoreScoped &&
            !SurfaceDiscovery.HasEventStoreArgument(surface.Value) &&
            string.IsNullOrWhiteSpace(classification.StoreArgumentException))
        .Select(surface => surface.Key)
        .Order()
        .ShouldBeEmpty();

    [Fact]
    void should_have_no_obsolete_store_argument_exceptions() => _surfaces
        .Where(surface => _classifications.TryGetValue(surface.Key, out var classification) &&
            classification.StoreArgumentException is not null &&
            SurfaceDiscovery.HasEventStoreArgument(surface.Value))
        .Select(surface => surface.Key)
        .ShouldBeEmpty();

    [Fact]
    void should_use_only_the_defined_entitlement_classes() => _classifications
        .Where(entry => entry.Value.Entitlement == Entitlement.None ||
            (!Enum.IsDefined(entry.Value.Entitlement) && entry.Value.Entitlement != (Entitlement.StoreRead | Entitlement.StoreAppend)))
        .Select(entry => entry.Key)
        .ShouldBeEmpty();
}
