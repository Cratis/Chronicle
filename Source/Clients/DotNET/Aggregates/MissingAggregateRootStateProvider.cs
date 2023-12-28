// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Aggregates;

/// <summary>
/// Exception that gets thrown when there is no state provider for a stateful aggregate root.
/// </summary>
public class MissingAggregateRootStateProvider : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MissingAggregateRootStateProvider"/> class.
    /// </summary>
    /// <param name="type">Aggregate root type that is missing a state provider.</param>
    public MissingAggregateRootStateProvider(Type type)
        : base($"Missing aggregate root state provider for {type.FullName}. You should either implement a reducer or an immediate projection for the state for this type")
    {
    }
}
