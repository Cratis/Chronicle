// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Storage.Registrations;

namespace Cratis.Chronicle.Registrations.for_RegistrationInbox;

public class when_the_storage_cannot_keep_intents : given.an_inbox
{
    RegistrationAcceptance _acceptance;

    protected override bool SupportsIntents => false;

    async Task Because() => _acceptance = await Accept([CreateDefinition("projection")], "studio");

    [Fact] void should_not_accept() => _acceptance.ShouldBeNull();
    [Fact] void should_not_touch_the_manager() => _manager.ReceivedCalls().ShouldBeEmpty();
}
