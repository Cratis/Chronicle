// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.when_adding;

public class with_a_failed_oauth_scope_batch : given.an_oauth_registration
{
    Exception? _exception;

    void Establish() => _sequence.AppendMany(Arg.Any<IEnumerable<EventToAppend>>(), Arg.Any<CorrelationId>(), Arg.Any<IEnumerable<Causation>>(), Arg.Any<Identity>(), Arg.Any<ConcurrencyScopes>())
        .Returns(new AppendManyResult { Errors = [new AppendError("synthetic failure")] });

    async Task Because() => _exception = await Catch.Exception(async () => await _registrar.Add("store", [_definition]));

    [Fact] void should_fail_the_registration() => _exception.ShouldBeOfExactType<WebhookRegistrationFailed>();
    [Fact] void should_keep_secrets_out_of_the_error() => _exception!.Message.ShouldNotContain("secret");
}
