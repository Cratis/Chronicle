// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookAdmission.when_writing;

public class with_a_failed_oauth_pair : given.an_envelope_write
{
    void Establish()
    {
        _authorization = new OAuthAuthorization("https://example.com", "client", "secret") { Scope = "scope" };
        _eventSequence.AppendMany(Arg.Any<IEnumerable<EventToAppend>>(), Arg.Any<CorrelationId>(), Arg.Any<IEnumerable<Causation>>(), Arg.Any<Identity>(), Arg.Any<ConcurrencyScopes>())
            .Returns(new AppendManyResult { Errors = [new AppendError("synthetic failure")] });
    }

    async Task Because() => _error = await Catch.Exception(Write);

    [Fact] void should_fail_the_registration() => _error.ShouldBeOfExactType<WebhookRegistrationFailed>();
    [Fact] void should_not_retry_the_pair() => AppendManyCount.ShouldEqual(1);
    [Fact] void should_not_mark_the_request_as_admitted() => _context.Admitted.ShouldBeEmpty();
}
