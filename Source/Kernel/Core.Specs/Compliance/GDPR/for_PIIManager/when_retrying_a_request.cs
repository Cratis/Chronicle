// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIIManager;

public class when_retrying_a_request : given.a_pii_manager
{
    int _attempts;
    List<string> _requests = [];
    bool _confirmedAfterGrace;

    void Establish()
    {
        _keyStore.RecordErasureFor(EventStore, EventStoreNamespace, Identifier, Arg.Any<EncryptionKeyErasureRequest>())
            .Returns(call =>
            {
                _requests.Add(call.Arg<EncryptionKeyErasureRequest>().RequestId);
                return ++_attempts == 1 ? Task.FromException(new EncryptionKeyFenceUnavailable(Identifier)) : Task.CompletedTask;
            });
        _keyStore.When(_ => _.ConfirmErasureFor(EventStore, EventStoreNamespace, Identifier, "opaque-request"))
            .Do(_ => _confirmedAfterGrace = _delays.Contains(TimeSpan.FromSeconds(5)));
    }

    Task Because() => _manager.DeleteEncryptionKeyInWithRequest(Identifier, [EventStore], "opaque-request");

    [Fact] void should_reuse_the_caller_identity() => _requests.ShouldEqual(new[] { "opaque-request", "opaque-request" });
    [Fact] void should_back_off_before_retrying() => (_delays[0] > TimeSpan.Zero && _delays[0] < TimeSpan.FromMilliseconds(30)).ShouldBeTrue();
    [Fact] void should_wait_for_the_release_grace_before_confirming() => _confirmedAfterGrace.ShouldBeTrue();
    [Fact] void should_sweep_only_the_request_boundary() => _keyStore.Received(1).DeleteThroughFor(EventStore, EventStoreNamespace, Identifier, "opaque-request");
    [Fact] void should_never_issue_an_unbounded_delete() => _keyStore.DidNotReceive().DeleteFor(EventStore, EventStoreNamespace, Identifier);
}
