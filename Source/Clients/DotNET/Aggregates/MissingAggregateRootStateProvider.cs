// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Aggregates;

/// <summary>
/// Exception that gets thrown when there is no state provider for a stateful aggregate root.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MissingAggregateRootStateProvider"/> class.
/// </remarks>
/// <param name="type">Aggregate root type that is missing a state provider.</param>
public class MissingAggregateRootStateProvider(Type type) : Exception($"Missing aggregate root state provider for {type.FullName}. You should either implement a reducer or a projection for the state for this type")
{
}
