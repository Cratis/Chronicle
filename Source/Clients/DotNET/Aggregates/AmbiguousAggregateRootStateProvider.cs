// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Aggregates;

/// <summary>
/// Exception that gets thrown when there is an ambiguous state provider for a stateful aggregate root.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AmbiguousAggregateRootStateProvider"/> class.
/// </remarks>
/// <param name="type">Aggregate root type that has an ambiguous state provider.</param>
public class AmbiguousAggregateRootStateProvider(Type type) : Exception($"Ambiguous aggregate root state provider for {type.FullName}. You should either implement a reducer or an immediate projection for the state for this type, not both")
{
}
