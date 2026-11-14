// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// The policy of a subject was set for one direction. The event source id is the subject.
/// </summary>
/// <param name="Direction">The direction the entry governs.</param>
/// <param name="EventStore">The local event store the subject's link addresses.</param>
/// <param name="Namespace">The namespace of that event store the subject's link addresses.</param>
/// <param name="AllPublic">Whether the entry covers every public event type.</param>
/// <param name="EventTypes">The specific event types allowed when <paramref name="AllPublic"/> is false.</param>
/// <param name="SetBy">The identity that set the policy.</param>
[EventType]
public record FederationPolicySet(FederationDirection Direction, string EventStore, string Namespace, bool AllPublic, IEnumerable<string> EventTypes, string SetBy);
