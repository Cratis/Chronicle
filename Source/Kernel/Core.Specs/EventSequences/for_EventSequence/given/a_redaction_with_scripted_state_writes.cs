// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using Orleans.TestKit;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

/// <summary>
/// A redaction of one event where the script names which grain state writes (opening, closing) fail.
/// </summary>
public class a_redaction_with_scripted_state_writes : a_content_mutation_with_observers
{
    protected ScriptedStateWrites _writes = new();
    protected RecordingEventSequenceLogger _logger = new();
    protected Exception? _exception;

    protected override async Task<EventSequence> CreateEventSequence()
    {
        _stateStorage = _silo.StorageManager.GetStorage<EventSequenceState>(typeof(EventSequenceWithScriptedStateWrites).FullName!);
        _stateStorage.State.ConstraintIndexWatermark = EventSequenceNumber.First;
        _silo.AddService(_writes);
        _silo.AddService<Microsoft.Extensions.Logging.ILogger<EventSequence>>(_logger);
        return await _silo.CreateGrainAsync<EventSequenceWithScriptedStateWrites>(_eventSequenceKey.ToString());
    }

    protected void MutationFails() =>
        _eventSequenceStorage.Redact(
            Arg.Any<EventSequenceNumber>(),
            Arg.Any<RedactionReason>(),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<Causation>>(),
            Arg.Any<IEnumerable<IdentityId>>(),
            Arg.Any<DateTimeOffset>()).Returns(Task.FromException<AppendedEvent>(new SimulatedStorageError()));

    protected async Task Redact() => _exception = await Catch.Exception(() => _eventSequence.Redact(42UL, "reason", CorrelationId.New(), [], Identity.System));
}
