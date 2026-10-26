// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.given;

public enum store_access
{
    Read,
    Append,
    Manage
}

public enum store_effect
{
    Reads,
    Appends,
    Configures,
    Destroys,
    Exports,
    CrossStore,
    CreatesDelegation,
    Stream,
    Enumerates
}

public record store_scoped_surface(string SurfaceId, IReadOnlySet<store_access> Access, IReadOnlySet<store_effect> Effects);

public static class store_scoped_surface_classification
{
    public static store_scoped_surface Read(string id, params store_effect[] effects) =>
        new(id, new HashSet<store_access> { store_access.Read }, effects.Prepend(store_effect.Reads).ToHashSet());

    public static store_scoped_surface Append(string id, params store_effect[] effects) =>
        new(id, new HashSet<store_access> { store_access.Append }, effects.ToHashSet());

    public static store_scoped_surface Manage(string id, params store_effect[] effects) =>
        new(id, new HashSet<store_access> { store_access.Manage }, effects.ToHashSet());

    public static store_scoped_surface ManageExport(string id) =>
        new(id, new HashSet<store_access> { store_access.Manage, store_access.Read },
            new HashSet<store_effect> { store_effect.Configures, store_effect.Exports });

    public static store_scoped_surface Observe(string id) =>
        new(id, new HashSet<store_access> { store_access.Read, store_access.Append },
            new HashSet<store_effect> { store_effect.Reads, store_effect.Appends, store_effect.Configures, store_effect.Stream });
}
