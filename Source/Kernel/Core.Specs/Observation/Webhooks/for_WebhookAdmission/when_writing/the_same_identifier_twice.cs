// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookAdmission.when_writing;

public class the_same_identifier_twice : given.an_envelope_write
{
    void Establish() => ALogWithLastLifecycleEvent(Cratis.Monads.Option<Concepts.Events.AppendedEvent>.None(), ALifecycleEvent<WebhookAdded>(3));

    async Task Because() => _error = await Catch.Exception(async () =>
    {
        await Write();
        await Write();
    });

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_build_changes_only_once() => _comparisons.ShouldEqual(1);
    [Fact] void should_append_only_once() => AppendCount.ShouldEqual(1);
    [Fact] void should_recheck_the_format_on_the_second_write() => LifecycleReads.ShouldEqual(2);
}
