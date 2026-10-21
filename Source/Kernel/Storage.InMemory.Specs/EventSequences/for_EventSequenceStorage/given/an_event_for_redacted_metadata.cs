// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.given;

public class an_event_for_redacted_metadata : a_metadata_storage
{
    protected CorrelationId _redactionCorrelation;
    protected Causation _redactionCausation;
    protected IdentityId _redactor;
    protected DateTimeOffset _redactionOccurred;

    async Task Establish()
    {
        await _storage.AppendMany([_entry]);
        _redactionCorrelation = CorrelationId.New();
        _redactionOccurred = _entry.Occurred.AddMinutes(1);
        _redactionCausation = new Causation(_redactionOccurred, "redaction", new Dictionary<string, string>());
        _redactor = IdentityId.New();
    }
}
