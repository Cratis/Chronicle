// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_ClosesStreamConstraintsProvider.when_providing;

public class and_reopen_event_lacks_property : given.a_closing_provider
{
    Exception _error;

    void Establish()
    {
        Register<Closed>();
        Register<Reopened>();
        _artifacts.ClosesStreamEventTypes.Returns([typeof(Closed)]);
    }

    void Because() => _error = Catch.Exception(() => _provider.Provide());

    [Fact] void should_refuse_the_invalid_reopening_type() => _error.ShouldBeOfExactType<PropertyDoesNotExistOnEventType>();
    [Fact] void should_identify_the_reopening_type() => ((PropertyDoesNotExistOnEventType)_error).EventType.Id.Value.ShouldEqual(nameof(Reopened));

    [ClosesStream(EventStreamIdFrom = "Period", ReopenedBy = [typeof(Reopened)])]
    record Closed(string Period);

    record Reopened();
}
