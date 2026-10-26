// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security;

public class when_inventorying_surface_classifications : Specification
{
    given.store_scoped_surface[] _surfaces;

    void Because() => _surfaces = given.store_scoped_surfaces.Commands.Values
        .Concat(given.store_scoped_surfaces.Queries.Values)
        .Concat(given.store_scoped_grpc_operations.All.Values)
        .ToArray();

    [Fact] void should_classify_a_nonempty_inventory() => _surfaces.ShouldNotBeEmpty();
    [Fact] void should_assign_at_least_one_access_class_to_every_surface() =>
        string.Join(", ", _surfaces.Where(surface => surface.Access.Count == 0).Select(surface => surface.SurfaceId)).ShouldEqual(string.Empty);
    [Fact] void should_describe_effects_for_every_surface() =>
        string.Join(", ", _surfaces.Where(surface => surface.Effects.Count == 0).Select(surface => surface.SurfaceId)).ShouldEqual(string.Empty);
    [Fact] void should_require_read_access_for_exports() =>
        string.Join(", ", _surfaces.Where(surface => surface.Effects.Contains(given.store_effect.Exports) && !surface.Access.Contains(given.store_access.Read)).Select(surface => surface.SurfaceId)).ShouldEqual(string.Empty);
    [Fact] void should_mark_delegation_creation_as_cross_store() =>
        string.Join(", ", _surfaces.Where(surface => surface.Effects.Contains(given.store_effect.CreatesDelegation) && !surface.Effects.Contains(given.store_effect.CrossStore)).Select(surface => surface.SurfaceId)).ShouldEqual(string.Empty);
}
