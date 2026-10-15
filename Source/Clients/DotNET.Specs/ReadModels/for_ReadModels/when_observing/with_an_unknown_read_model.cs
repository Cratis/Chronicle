// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_observing;

public class with_an_unknown_read_model : given.a_collection_stream
{
    Exception _failure;

    void Establish() => _projections.HasFor(Arg.Is<Type>(type => type == typeof(Row))).Returns(false);
    void Because() => _failure = Catch.Exception(() => _readModels.Observe<Row>());

    [Fact] void should_refuse_an_unknown_read_model() => _failure.ShouldBeOfExactType<UnknownReadModel>();
}
