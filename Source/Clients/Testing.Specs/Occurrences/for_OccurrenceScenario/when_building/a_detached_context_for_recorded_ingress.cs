// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Occurrences;

namespace Cratis.Chronicle.Testing.Occurrences.for_OccurrenceScenario.when_building;

public class a_detached_context_for_recorded_ingress : given.a_scenario
{
    OccurrenceContext _context;

    void Because()
    {
        _scenario.Time.Advance(TimeSpan.FromDays(1));
        _context = new OccurrenceContextBuilder(_scenario.Time).For<ImportRequested>().WithIdempotencyId("request-one").BuildDetached();
    }

    [Fact] void should_derive_the_same_identity_as_kernel_ingress() => _context.Id.ShouldEqual(_recorded.Id);
    [Fact] void should_read_the_supplied_time_driver() => _context.RecordedAt.ShouldEqual(_scenario.Time.Now);
    [Fact] void should_default_to_the_first_attempt() => _context.Attempt.ShouldEqual(1);
    [Fact] void should_default_to_the_first_epoch() => _context.Lease.Epoch.ShouldEqual(1L);
}
