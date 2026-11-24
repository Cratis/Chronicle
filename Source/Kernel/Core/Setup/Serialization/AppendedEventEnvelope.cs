// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// What an <see cref="AppendedEvent"/> is written as between grains: its context and bookkeeping as Orleans values, and its
/// content as JSON text next to the schema that reads it back into typed values.
/// </summary>
/// <param name="Context">The <see cref="EventContext"/> of the event.</param>
/// <param name="Content">The content of the event as JSON.</param>
/// <param name="Schema">The schema of the event type as JSON, used to read the content back.</param>
/// <param name="OriginalContent">The content as it was before any redaction.</param>
/// <param name="Revisions">The <see cref="EventRevision">revisions</see> of the event.</param>
/// <param name="GenerationalContent">The content of the event per generation.</param>
/// <param name="GenerationalHashes">The <see cref="EventHash">hash</see> of the event per generation.</param>
/// <param name="RevisedGeneration">The generation the event was revised to, if any.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Setup.Serialization.AppendedEventEnvelope")]
internal sealed record AppendedEventEnvelope(
    [property: Id(0)] EventContext Context,
    [property: Id(1)] string Content,
    [property: Id(2)] string Schema,
    [property: Id(3)] string OriginalContent,
    [property: Id(4)] EventRevision[] Revisions,
    [property: Id(5)] Dictionary<int, string> GenerationalContent,
    [property: Id(6)] Dictionary<int, EventHash> GenerationalHashes,
    [property: Id(7)] EventTypeGeneration? RevisedGeneration);
