// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Namespaces.for_NamespacesReactor.when_a_namespace_is_added;

/// <summary>
/// Tags are part of a seeded fact's identity. Global entries with the same source, type and content but different
/// tags must both reach a namespace and retain the tag that distinguishes each one.
/// </summary>
public class and_global_events_differ_only_by_tags : given.a_namespaces_reactor
{
    void Establish() => GlobalSeeds(AnEntry("internal"), AnEntry("external"));
    async Task Because() => await AddNamespace();

    [Fact] void should_offer_both_facts() => EntriesOfferedToTheNamespace.Count().ShouldEqual(2);
    [Fact] void should_keep_the_internal_tag() => EntriesOfferedToTheNamespace.Any(_ => _.Tags?.Any(tag => tag.Value == "internal") == true).ShouldBeTrue();
    [Fact] void should_keep_the_external_tag() => EntriesOfferedToTheNamespace.Any(_ => _.Tags?.Any(tag => tag.Value == "external") == true).ShouldBeTrue();
    [Fact] void should_keep_the_order_the_seeder_wrote() => EntriesOfferedToTheNamespace.Select(_ => _.Tags!.Single().Value).ToArray().ShouldEqual<string[]>(["internal", "external"]);
}
