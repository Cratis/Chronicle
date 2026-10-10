// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Events.Constraints.for_ClosesStreamConstraintsProvider.when_providing;

public class and_same_named_dimensions_conflict : given.a_closing_provider
{
    Exception _error;

    void Establish()
    {
        Register<Closed>();
        Register<Cancelled>();
        _artifacts.ClosesStreamEventTypes.Returns([typeof(Closed), typeof(Cancelled)]);
    }

    void Because() => _error = Catch.Exception(() => _provider.Provide());

    [Fact] void should_refuse_conflicting_scopes() => _error.ShouldBeOfExactType<ConflictingClosesStreamDeclarations>();

    [ClosesStream(Name = "shared", Dimensions = ClosedStreamDimensions.EventSourceId)]
    record Closed();

    [ClosesStream(Name = "shared", Dimensions = ClosedStreamDimensions.EventStreamId)]
    record Cancelled();
}
