// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventTypes.for_EventTypeDetailsConverters.when_converting_a_registration;

public class of_a_collection : given.a_registration
{
    EventTypeDetails[] _result;

    void Because() => _result = [.. new[]
    {
        RegistrationWith(EventTypeVisibility.Public, "other-store"),
        RegistrationWith(EventTypeVisibility.Private),
        RegistrationWith(EventTypeVisibility.Unspecified)
    }.ToReadModel()];

    [Fact] void should_convert_every_registration() => _result.Length.ShouldEqual(3);
    [Fact] void should_preserve_each_visibility() => _result.Select(_ => _.Visibility).ShouldContainOnly(
        Contracts.Events.EventTypeVisibility.Public,
        Contracts.Events.EventTypeVisibility.Private,
        Contracts.Events.EventTypeVisibility.Unspecified);
    [Fact] void should_preserve_each_origin() => _result.Select(_ => _.Origin).ShouldContainOnly("other-store", string.Empty, string.Empty);
}
