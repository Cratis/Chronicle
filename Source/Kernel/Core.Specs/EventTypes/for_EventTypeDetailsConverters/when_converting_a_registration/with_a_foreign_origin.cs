// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventTypes.for_EventTypeDetailsConverters.when_converting_a_registration;

public class with_a_foreign_origin : given.a_registration
{
    EventTypeDetails _result;

    void Establish() => _registration = RegistrationWith(EventTypeVisibility.Public, "other-store");

    void Because() => _result = _registration.ToReadModel();

    [Fact] void should_carry_the_origin() => _result.Origin.ShouldEqual("other-store");
    [Fact] void should_carry_the_visibility() => _result.Visibility.ShouldEqual(Contracts.Events.EventTypeVisibility.Public);
}
