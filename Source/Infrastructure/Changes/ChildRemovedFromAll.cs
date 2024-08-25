// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes;

/// <summary>
/// Represents a child that has been removed from all models that has a match on identity.
/// </summary>
/// <param name="ChildrenProperty">The property holding the children in the parent object.</param>
/// <param name="IdentifiedByProperty">The property that identifies the key on the child object.</param>
/// <param name="Key">Key of the object.</param>
/// <param name="ArrayIndexers">All <see cref="ArrayIndexer">array indexers</see>.</param>
public record ChildRemovedFromAll(PropertyPath ChildrenProperty, PropertyPath IdentifiedByProperty, object Key, ArrayIndexers ArrayIndexers) : Change("[n/a]");
