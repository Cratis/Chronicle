// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_validating_an_import_chunk;

public class and_every_event_is_valid : given.an_event_sequence_with_imports
{
    async Task Because() => _validation = await _eventSequence.ValidateImportChunk(_importId, true, ImportEvents());

    [Fact] void should_reject_nothing() => _validation.Rejections.ShouldBeEmpty();
    [Fact] void should_write_nothing() => _writes.ShouldEqual(0);
    [Fact] void should_create_no_encryption_key() => _complianceManager.DidNotReceiveWithAnyArgs().ApplyReportingKeys(default!, default!, default!, default!, default!);
}
