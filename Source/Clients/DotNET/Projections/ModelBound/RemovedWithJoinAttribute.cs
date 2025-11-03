// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Attribute used to indicate what event removes a child from a collection through a join.
/// </summary>
/// <typeparam name="TEvent">The type of event that removes the child.</typeparam>
/// <remarks>
/// Initializes a new instance of <see cref="RemovedWithJoinAttribute{TEvent}"/>.
/// </remarks>
/// <param name="key">Optional property name on the event that identifies the child to remove. Defaults to WellKnownExpressions.EventSourceId.</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = true)]
public sealed class RemovedWithJoinAttribute<TEvent>(string? key = default) : Attribute, IProjectionAnnotation
{
    /// <summary>
    /// Gets the property name on the event that identifies the child to remove.
    /// </summary>
    public string Key { get; } = key ?? WellKnownExpressions.EventSourceId;
}
