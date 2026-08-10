// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Namespaces.for_NamespacesReactor.when_a_namespace_is_added;

/// <summary>
/// Two equal entries in global seeding state are two facts. Replaying that state into a namespace must preserve
/// both occurrences rather than collapsing them by event source, type and content.
/// </summary>
public class and_an_identical_global_event_is_repeated : given.a_namespaces_reactor
{
    void Establish() => GlobalSeeds(AnEntry(), AnEntry());
    async Task Because() => await AddNamespace();

    [Fact] void should_offer_both_facts() => EntriesOfferedToTheNamespace.Count().ShouldEqual(2);
    [Fact] void should_keep_both_payloads_unchanged() => EntriesOfferedToTheNamespace.All(_ => _.Content == "{\"month\":5}").ShouldBeTrue();
}
