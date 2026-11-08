// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Globalization;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Imports;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.Imports;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class an_event_sequence_with_imports : an_event_sequence_with_idempotency_keys
{
    protected ImportId _importId = ImportId.New();
    protected ImportChunkAppendResult _chunkResult;
    protected ImportChunkValidationResult _validation;
    protected HashSet<EventSourceId> _rejectedSources = [];

    void Establish() => _constraintValidation.Establish(
        Arg.Any<EventSourceId>(),
        Arg.Any<EventTypeId>(),
        Arg.Any<ExpandoObject>(),
        Arg.Any<EventSourceType?>(),
        Arg.Any<EventStreamType?>(),
        Arg.Any<EventStreamId?>(),
        Arg.Any<ConstraintBatchClaims?>(),
        Arg.Any<Subject?>(),
        Arg.Any<IReadOnlyCollection<string>?>())
        .Returns(call => _rejectedSources.Contains(call.ArgAt<EventSourceId>(0))
            ? new ConstraintValidationContext([new RejectingConstraintValidator()], call.ArgAt<EventSourceId>(0), _eventType.Id, call.Arg<ExpandoObject>())
            : new ConstraintValidationContext([], call.ArgAt<EventSourceId>(0), _eventType.Id, call.Arg<ExpandoObject>()));

    protected IReadOnlyList<ImportEventToAppend> ImportEvents(IReadOnlyList<IdempotencyKey?>? keys = null) =>
        _events.Select((@event, index) => new ImportEventToAppend(
            index,
            @event with { Causation = [ImportCausation(index)] },
            keys?[index],
            null,
            null)).ToArray();

    protected Causation ImportCausation(long index) => new(
        DateTimeOffset.UtcNow,
        CausationType.Import,
        new Dictionary<string, string>
        {
            ["importId"] = _importId.Value.ToString("D", CultureInfo.InvariantCulture),
            ["chunkIndex"] = "0",
            ["eventIndex"] = index.ToString(CultureInfo.InvariantCulture)
        });

    protected Task<ImportChunkAppendResult> AppendChunk(IReadOnlyList<IdempotencyKey?>? keys = null, EventSequenceNumber? committedFrom = null) =>
        _eventSequence.AppendImportChunk(_importId, 0, committedFrom ?? EventSequenceNumber.First, [_eventType.Id], ImportEvents(keys));
}
