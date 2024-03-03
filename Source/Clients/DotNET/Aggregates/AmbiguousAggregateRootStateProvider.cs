// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Aggregates;

/// <summary>
/// Exception that gets thrown when there is an ambiguous state provider for a stateful aggregate root.
/// </summary>
public class AmbiguousAggregateRootStateProvider : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AmbiguousAggregateRootStateProvider"/> class.
    /// </summary>
    /// <param name="type">Aggregate root type that has an ambiguous state provider.</param>
    public AmbiguousAggregateRootStateProvider(Type type)
        : base($"Ambiguous aggregate root state provider for {type.FullName}. You should either implement a reducer or an immediate projection for the state for this type, not both")
    {
    }
}
