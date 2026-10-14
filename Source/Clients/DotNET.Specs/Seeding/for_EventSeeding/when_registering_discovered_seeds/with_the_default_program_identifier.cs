// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Seeding.for_EventSeeding.when_registering_discovered_seeds;

public class with_the_default_program_identifier : given.a_seeding_builder_with_discovered_globals
{
    async Task Establish()
    {
        _seeding.UseGlobalSetOwner("[N/A]");
        await _seeding.Discover();
    }

    Task Because() => _seeding.RegisterDiscovered();

    [Fact] void should_use_the_entry_assembly_as_owner() => _requests.Single(_ => _.GlobalSetIsAuthoritative).GlobalSetOwner.ShouldEqual(Assembly.GetEntryAssembly()!.GetName().Name);
}
