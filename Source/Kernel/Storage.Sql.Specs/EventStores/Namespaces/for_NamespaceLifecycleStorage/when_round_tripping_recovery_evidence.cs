// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.for_NamespaceLifecycleStorage;

public class when_round_tripping_recovery_evidence : given.a_lifecycle_storage
{
    NamespaceLifecycle _state;
    NamespaceLifecycle? _result;
    bool _written;

    void Establish() => _state = Open() with
    {
        PreviousIncarnation = NamespaceIncarnationId.New(),
        NotifyOnOpen = true,
        Status = NamespaceLifecycleStatus.RecoveryRequired,
        OperationId = Guid.NewGuid(),
        Stage = NamespaceRemovalStage.Drop,
        StageAttempts = 2,
        TotalAttempts = 4,
        AttemptInProgress = true,
        DestructiveAttemptOutstanding = true,
        DestructiveActionIssued = true,
        LastAttemptAt = _now.AddSeconds(1),
        RetryAt = _now.AddMinutes(1),
        Deadline = _now.AddSeconds(31),
        LastFailure = "NamespaceRemovalOutcomeUnknown"
    };

    async Task Because()
    {
        _written = await _storage.TryWrite(_name, null, _state);
        _result = await CreateStorage().Get("oRDERS");
    }

    [Fact] void should_acknowledge_the_insert() => _written.ShouldBeTrue();
    [Fact] void should_preserve_every_field_from_a_fresh_storage() => _result.ShouldEqual(_state);
    [Fact] void should_preserve_the_display_name() => _result!.Namespace.Value.ShouldEqual("Orders");
}
