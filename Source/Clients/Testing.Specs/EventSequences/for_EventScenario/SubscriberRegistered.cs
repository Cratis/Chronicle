// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

/// <summary>
/// Event carrying a model-bound <see cref="UniqueAttribute"/> on a <see cref="SubscriberEmail"/> (a <c>ConceptAs&lt;string&gt;</c>) property.
/// </summary>
/// <param name="Email">The subscriber's email address; unique across event sources.</param>
[EventType]
public record SubscriberRegistered([property: Unique(SubscriberRegistered.UniqueEmailConstraint)] SubscriberEmail Email)
{
    /// <summary>
    /// The name of the uniqueness constraint on <see cref="Email"/>.
    /// </summary>
    public const string UniqueEmailConstraint = "UniqueSubscriberEmail";
}
