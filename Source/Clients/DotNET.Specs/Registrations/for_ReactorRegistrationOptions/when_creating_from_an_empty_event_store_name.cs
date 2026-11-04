// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations.for_ReactorRegistrationOptions;

public class when_creating_from_an_empty_event_store_name : Specification
{
    Exception[] _errors;

    void Because() => _errors = new string?[] { null, "", "  " }
        .Select(_ => Catch.Exception(() => ReactorRegistrationOptions.FromEventStore(_ is null ? null! : new EventStoreName(_)))!).ToArray();

    [Fact] void should_reject_a_null_name() => _errors[0].ShouldBeAssignableFrom<ArgumentException>();
    [Fact] void should_reject_an_empty_name() => _errors[1].ShouldBeOfExactType<ArgumentException>();
    [Fact] void should_reject_a_whitespace_name() => _errors[2].ShouldBeOfExactType<ArgumentException>();
}
