// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Seeding.for_EventSeeding.when_registering_discovered_seeds;

public class with_a_program_identifier : given.a_seeding_builder_with_discovered_globals
{
    async Task Establish()
    {
        _seeding.UseGlobalSetOwner("my-program");
        _seeding.For("manual", [new TestEvent("manual")]);
        await _seeding.Discover();
    }

    Task Because() => _seeding.RegisterDiscovered();

    [Fact] void should_declare_discovered_globals_with_the_owner() => _requests.Single(_ => _.GlobalSetIsAuthoritative).GlobalSetOwner.ShouldEqual("my-program");
    [Fact] void should_not_include_manual_globals_in_the_declaration() => _requests.Single(_ => _.GlobalSetIsAuthoritative).GlobalByEventSource.Select(_ => _.EventSourceId).ShouldContainOnly(["discovered"]);
    [Fact] void should_add_manual_globals_without_replacing_the_set() => _requests.Single(_ => !_.GlobalSetIsAuthoritative).GlobalByEventSource.Select(_ => _.EventSourceId).ShouldContainOnly(["manual"]);
    [Fact] void should_own_explicit_additions_too() => _requests.Single(_ => !_.GlobalSetIsAuthoritative).GlobalSetOwner.ShouldEqual("my-program");
}
