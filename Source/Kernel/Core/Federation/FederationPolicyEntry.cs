// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Represents what a subject may do in one direction: the target it addresses and the event types allowed.
/// </summary>
/// <remarks>
/// The target is decided here, by the operator, and never by the certificate or the request. The ceiling is always the set
/// of public event types: no entry, however broad, exposes a private or unspecified event type.
/// </remarks>
/// <param name="Direction">The direction the entry governs.</param>
/// <param name="EventStore">The local event store the subject's link addresses.</param>
/// <param name="Namespace">The namespace of that event store the subject's link addresses.</param>
/// <param name="AllPublic">Whether the entry covers every public event type.</param>
/// <param name="EventTypes">The specific event types allowed when <paramref name="AllPublic"/> is false.</param>
public record FederationPolicyEntry(
    FederationDirection Direction,
    string EventStore,
    string Namespace,
    bool AllPublic,
    IReadOnlyList<string> EventTypes);
