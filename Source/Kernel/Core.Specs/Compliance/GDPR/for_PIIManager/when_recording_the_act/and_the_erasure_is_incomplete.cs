// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIIManager.when_recording_the_act;

/// <summary>
/// The failure mode a compliance log has to avoid is a partial erasure that reads like a completed one. An
/// incomplete erasure is a different message at a different level, and it says outright that the subject is not
/// erased.
/// </summary>
public class and_the_erasure_is_incomplete : given.a_pii_manager
{
    void Establish() =>
        _keyStore
            .DeleteFor(EventStore, EventStoreNamespace, Identifier)
            .ThrowsAsync(new StoreUnreachable());

    async Task Because() => await Catch.Exception(() => _manager.DeleteEncryptionKeyFor(Identifier));

    [Fact] void should_record_the_act() => _logger.Entries.ShouldNotBeEmpty();
    [Fact] void should_record_it_as_a_failure() => _logger.Entries[0].Level.ShouldEqual(LogLevel.Error);
    [Fact] void should_say_the_subject_is_not_erased() => _logger.Entries[0].Message.ShouldContain("not erased");
    [Fact] void should_not_read_as_a_completed_erasure() => _logger.Entries[0].Message.ShouldNotContain("Erased the encryption key");
    [Fact] void should_record_how_many_operations_failed() => _logger.Entries[0].Message.ShouldContain("1 operation(s) failed");
    [Fact] void should_record_the_subject_as_a_binding() => _logger.Entries[0].Message.ShouldContain(SubjectBinding);
    [Fact] void should_not_log_any_key_material() => ShouldNotHaveLoggedAnyKeyMaterial();
}
