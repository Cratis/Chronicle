// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Specifies the event sequence to use as source for the projection.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="FromEventSequenceAttribute"/> class.
/// </remarks>
/// <param name="eventSequenceId">The event sequence identifier.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
public sealed class FromEventSequenceAttribute(string eventSequenceId) : Attribute, IProjectionAnnotation
{
    /// <summary>
    /// Gets the event sequence identifier.
    /// </summary>
    public EventSequenceId EventSequenceId { get; } = new EventSequenceId(eventSequenceId);
}
