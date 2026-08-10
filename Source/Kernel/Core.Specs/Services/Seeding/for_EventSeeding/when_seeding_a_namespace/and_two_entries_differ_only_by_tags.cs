// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Seeding;

namespace Cratis.Chronicle.Services.Seeding.for_EventSeeding.when_seeding_a_namespace;

/// <summary>
/// Tags remain part of a seed entry's identity at the namespace-specific service boundary. Equal source, type
/// and content with different tags represent two distinct facts and must both reach the namespace grain.
/// </summary>
public class and_two_entries_differ_only_by_tags : given.an_event_seeding_service
{
    SeedRequest _request;

    void Establish() => _request = ANamespacedRequestFor(
        AnEntry("timesheet-1", "submitted", /*lang=json,strict*/ "{\"month\":5}", "internal"),
        AnEntry("timesheet-1", "submitted", /*lang=json,strict*/ "{\"month\":5}", "external"));

    async Task Because() => await _service.Seed(_request);

    [Fact] void should_keep_both_entries() => EntriesSeededForTheNamespace.Count().ShouldEqual(2);
    [Fact] void should_keep_the_internal_one() => EntriesSeededForTheNamespace.Any(_ => _.Tags?.Any(tag => tag.Value == "internal") == true).ShouldBeTrue();
    [Fact] void should_keep_the_external_one() => EntriesSeededForTheNamespace.Any(_ => _.Tags?.Any(tag => tag.Value == "external") == true).ShouldBeTrue();
    [Fact] void should_keep_the_order_the_seeder_wrote() => EntriesSeededForTheNamespace.Select(_ => _.Tags!.Single().Value).ToArray().ShouldEqual<string[]>(["internal", "external"]);
}
