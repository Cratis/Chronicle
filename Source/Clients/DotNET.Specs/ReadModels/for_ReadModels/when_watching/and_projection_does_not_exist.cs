// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_watching;

public class and_projection_does_not_exist : given.all_dependencies
{
    class MyReadModel
    {
        public string Name { get; set; }
    }

    Exception _result;

    void Establish() => _projections.HasFor<MyReadModel>().Returns(false);

    void Because() => _result = Catch.Exception(() => _readModels.Watch<MyReadModel>());

    [Fact] void should_throw_unknown_read_model() => _result.ShouldBeOfExactType<UnknownReadModel>();
}
