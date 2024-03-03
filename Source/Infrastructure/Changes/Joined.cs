// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Properties;

namespace Cratis.Changes;

/// <summary>
/// Represents a join.
/// </summary>
/// <param name="State">State of the object being joined.</param>
/// <param name="Key">The key used for the join.</param>
/// <param name="OnProperty">The property being joined.</param>
/// <param name="ArrayIndexers">All <see cref="ArrayIndexer">array indexers</see>.</param>
/// <param name="Changes">Changes applicable for the join change.</param>
public record Joined(object State, object Key, PropertyPath OnProperty, ArrayIndexers ArrayIndexers, IEnumerable<Change> Changes) : Change(State);
