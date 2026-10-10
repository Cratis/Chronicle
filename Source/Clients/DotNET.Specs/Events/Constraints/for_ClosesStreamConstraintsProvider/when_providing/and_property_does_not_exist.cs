// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_ClosesStreamConstraintsProvider.when_providing;

public class and_property_does_not_exist : given.a_closing_provider
{
    Exception _error;

    void Establish()
    {
        Register<Closed>();
        _artifacts.ClosesStreamEventTypes.Returns([typeof(Closed)]);
    }

    void Because() => _error = Catch.Exception(() => _provider.Provide());

    [Fact] void should_refuse_the_invalid_declaration() => _error.ShouldBeOfExactType<PropertyDoesNotExistOnEventType>();

    [ClosesStream(EventStreamIdFrom = "Missing")]
    record Closed(string Period);
}
