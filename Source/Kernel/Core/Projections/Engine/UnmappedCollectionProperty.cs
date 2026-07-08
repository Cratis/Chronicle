// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// Represents a collection read-model property that AutoMaps to nothing and will always project empty.
/// </summary>
/// <param name="Property">The name of the collection property on the read model (or child).</param>
/// <param name="EventTypes">The source event type(s) inspected for a matching property.</param>
internal sealed record UnmappedCollectionProperty(string Property, string EventTypes);
