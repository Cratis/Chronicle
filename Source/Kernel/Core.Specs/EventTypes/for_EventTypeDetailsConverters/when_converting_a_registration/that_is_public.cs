// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventTypes.for_EventTypeDetailsConverters.when_converting_a_registration;

public class that_is_public : given.a_registration
{
    EventTypeDetails _result;

    void Establish() => _registration = RegistrationWith(EventTypeVisibility.Public);

    void Because() => _result = _registration.ToReadModel();

    [Fact] void should_carry_the_visibility() => _result.Visibility.ShouldEqual(Contracts.Events.EventTypeVisibility.Public);
    [Fact] void should_have_empty_origin() => _result.Origin.ShouldEqual(string.Empty);
    [Fact] void should_keep_the_identity() => _result.Id.ShouldEqual("some-type");
    [Fact] void should_keep_the_generation() => _result.Type.Generation.ShouldEqual(2u);
}
