// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookAdmission.when_writing;

public class a_changed_oauth_scope_with_a_lifecycle_race : given.an_envelope_write
{
    void Establish()
    {
        ALogWithLastLifecycleEvent(ALifecycleEvent<WebhookAdded>(3));
        ExistingWebhookWithFormat(WebhookDeliveryFormat.EnvelopeV1);
        _events = [];
        _authorization = new OAuthAuthorization("https://example.com", "client", "secret") { Scope = "scope" };
        _eventSequence.AppendMany(Arg.Any<IEnumerable<EventToAppend>>(), Arg.Any<CorrelationId>(), Arg.Any<IEnumerable<Causation>>(), Arg.Any<Identity>(), Arg.Any<ConcurrencyScopes>())
            .Returns(new AppendManyResult { ConcurrencyViolations = [new ConcurrencyViolation("webhook", 3, 7)] });
    }

    async Task Because() => _error = await Catch.Exception(Write);

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<WebhookLifecycleChangedDuringRegistration>();
    [Fact] void should_fence_the_pair_against_the_existing_lifecycle() => ((ConcurrencyScopes)_eventSequence.ReceivedCalls().Single(_ => _.GetMethodInfo().Name == "AppendMany").GetArguments()[4]!).GetFor("webhook").SequenceNumber.ShouldEqual((EventSequenceNumber)3);
    [Fact] void should_not_retry_the_pair() => AppendManyCount.ShouldEqual(1);
    [Fact] void should_not_retry_the_lifecycle_read() => LifecycleReads.ShouldEqual(1);
}
