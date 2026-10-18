// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventSequences.Idempotency;

/// <summary>
/// The plaintext intent, with routing already resolved and the subject defaulted to the event source id.
/// Contains protected plaintext: never serialize or log it. Attempt metadata deliberately has no place here.
/// </summary>
/// <param name="EventType">The requested type and generation.</param>
/// <param name="Content">The unprotected request content.</param>
/// <param name="EventSourceId">The resolved event source id.</param>
/// <param name="EventSourceType">The resolved source type.</param>
/// <param name="EventStreamType">The resolved stream type.</param>
/// <param name="EventStreamId">The resolved stream id.</param>
/// <param name="EventSource">The optional event source definition name.</param>
/// <param name="Subject">The effective subject.</param>
/// <param name="Identity">The append-key: identity used as the HKDF salt.</param>
/// <param name="ProtectedValues">Boundaries located by the compliance traversal.</param>
internal record AppendFingerprintInput(
    EventType EventType,
    JsonObject Content,
    EventSourceId EventSourceId,
    EventSourceType EventSourceType,
    EventStreamType EventStreamType,
    EventStreamId EventStreamId,
    EventSourceName? EventSource,
    string Subject,
    string Identity,
    IReadOnlyList<ProtectedValueLocation> ProtectedValues);
