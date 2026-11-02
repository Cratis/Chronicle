// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookAdmission.when_writing;

public class while_the_lifecycle_interleaves : given.an_envelope_write
{
    List<string> _steps;

    void Establish()
    {
        _steps = [];
        _storage.GetEventStore(Arg.Any<EventStoreName>()).Observers.Has(Arg.Any<ObserverId>()).Returns(_ =>
        {
            _steps.Add("history");

            // A competing add/replay/removal after the lifecycle read must invalidate the append, not trigger a retry.
            TheScopedAppendFailsWithAConcurrencyViolation();
            return false;
        });
        _authorization = new BearerTokenAuthorization("token");
    }

    async Task Because() => _error = await Catch.Exception(Write);

    [Fact] void should_check_history() => _steps.ShouldEqual(["history"]);
    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<WebhookLifecycleChangedDuringRegistration>();
    [Fact] void should_read_the_lifecycle_only_once() => LifecycleReads.ShouldEqual(1);
    [Fact] void should_attempt_only_one_append() => AppendCount.ShouldEqual(1);
    [Fact] void should_not_append_authorization() => UnscopedAppendCount.ShouldEqual(0);
    [Fact] void should_not_mark_the_failed_write_as_admitted() => _context.Admitted.ShouldBeEmpty();
}
