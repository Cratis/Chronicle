// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Applications.Aggregates;

/// <summary>
/// The exception that is thrown when an aggregate root cannot be resolved from the current command context.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="UnableToResolveAggregateRootFromCommandContext"/> class.
/// </remarks>
/// <param name="aggregateRootType">The type of aggregate root that could not be resolved.</param>
public class UnableToResolveAggregateRootFromCommandContext(Type aggregateRootType) :
    Exception($"Unable to resolve aggregate root of type '{aggregateRootType.FullName}' from current command context. Ensure that the command has either one property that is of derivative of EventSourceId or a property marked with [Key] attribute and that a command context is available.")
{
}
