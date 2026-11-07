// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Architecture.for_StoreScopedSurfaceInventory;

public class when_resolving_the_runtime_access : Specification
{
    KeyValuePair<string, MethodInfo>[] _surfaces;
    string[] _mismatches;

    void Establish() => _surfaces = [.. SurfaceDiscovery.Discover().Where(surface => surface.Key.StartsWith("Arc:", StringComparison.Ordinal))];

    void Because() => _mismatches = [.. _surfaces
        .Where(surface => SurfaceInventory.Classifications.TryGetValue(surface.Key, out var classification) && classification.IsStoreScoped)
        .Select(surface => (surface.Key, Expected: Expected(SurfaceInventory.Classifications[surface.Key]), Actual: Actual(surface.Value)))
        .Where(entry => entry.Expected != entry.Actual)
        .Select(entry => $"{entry.Key}: inventory {entry.Expected}, runtime {entry.Actual}")
        .Order()];

    [Fact] void should_resolve_the_same_access_as_the_inventory_for_every_store_scoped_entry_point() => string.Join(Environment.NewLine, _mismatches).ShouldEqual(string.Empty);

    static StoreAccess Expected(Classification classification)
    {
        var access = StoreAccess.None;
        if (classification.Entitlement.HasFlag(Entitlement.StoreRead)) access |= StoreAccess.Read;
        if (classification.Entitlement.HasFlag(Entitlement.StoreAppend)) access |= StoreAccess.Append;
        if (classification.Entitlement.HasFlag(Entitlement.StoreManage)) access |= StoreAccess.Manage;
        return access;
    }

    static StoreAccess Actual(MethodInfo method) => method.DeclaringType.IsDefined(typeof(CommandAttribute))
        ? StoreAccessMap.ForCommand(method.DeclaringType)
        : StoreAccess.Read;
}
