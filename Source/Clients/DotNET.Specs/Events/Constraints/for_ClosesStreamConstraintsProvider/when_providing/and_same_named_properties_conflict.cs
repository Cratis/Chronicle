// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_ClosesStreamConstraintsProvider.when_providing;

public class and_same_named_properties_conflict : given.a_closing_provider
{
    Exception _error;

    void Establish()
    {
        Register<Closed>();
        Register<Cancelled>();
        _artifacts.ClosesStreamEventTypes.Returns([typeof(Closed), typeof(Cancelled)]);
    }

    void Because() => _error = Catch.Exception(() => _provider.Provide());

    [Fact] void should_refuse_conflicting_payload_properties() => _error.ShouldBeOfExactType<ConflictingClosesStreamDeclarations>();

    [ClosesStream(Name = "shared", EventStreamIdFrom = "Period")]
    record Closed(string Period);

    [ClosesStream(Name = "shared", EventStreamIdFrom = "PreviousPeriod")]
    record Cancelled(string PreviousPeriod);
}
