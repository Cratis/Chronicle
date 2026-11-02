// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.when_adding;

public class retrying_after_a_failed_authorization_append : given.a_lagging_legacy_registration
{
    Exception? _firstError;
    int _authorizations;

    void Establish() => _eventSequence.Append(
        Arg.Any<EventSourceId>(),
        Arg.Any<object>(),
        Arg.Any<CorrelationId>(),
        Arg.Any<IEnumerable<Causation>>(),
        Arg.Any<Identity>(),
        Arg.Any<IEnumerable<Tag>>(),
        Arg.Any<EventSourceType>(),
        Arg.Any<EventStreamType>(),
        Arg.Any<EventStreamId>()).Returns(call =>
        {
            RecordAppend(call[1]);
            return call[1] is BearerTokenAuthorizationSetForWebhook && ++_authorizations == 1
                ? new AppendResult { Errors = [new AppendError("synthetic failure")] }
                : AppendResult.Success(CorrelationId.New(), EventSequenceNumber.First);
        });

    /// <summary>Source equivalence: base 130c50244a WebhookRegistrar.cs:85-109 and :421-428 retry New and its authorization unconditionally.</summary>
    /// <returns>Awaitable task.</returns>
    async Task Because()
    {
        _firstError = await Catch.Exception(() => _registrar.Add("store", [_definition]));
        _error = await Catch.Exception(() => _registrar.Add("store", [_definition]));
    }

    [Fact] void should_report_the_first_failure() => _firstError.ShouldBeOfExactType<WebhookRegistrationFailed>();
    [Fact] void should_succeed_on_retry() => _error.ShouldBeNull();
    [Fact] void should_append_both_registrations_and_authorizations() => _appended.Select(_ => _.GetType()).ShouldEqual([typeof(WebhookAdded), typeof(BearerTokenAuthorizationSetForWebhook), typeof(WebhookAdded), typeof(BearerTokenAuthorizationSetForWebhook)]);
    [Fact] void should_not_fence_either_write() => ScopedAppends.ShouldBeEmpty();
}
