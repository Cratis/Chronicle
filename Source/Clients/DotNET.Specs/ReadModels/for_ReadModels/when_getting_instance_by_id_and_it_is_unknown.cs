// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModels;

public class when_getting_instance_by_id_and_it_is_unknown : given.all_dependencies
{
    class MyReadModel
    {
        public string Name { get; set; }
    }

    ReadModelKey _key;
    Exception _exception;

    void Establish()
    {
        _key = "test-key";

        _projections.HasFor(typeof(MyReadModel)).Returns(false);
#pragma warning disable CA2263 // Prefer generic overload when type is known
        _reducers.HasFor(typeof(MyReadModel)).Returns(false);
#pragma warning restore CA2263 // Prefer generic overload when type is known
    }

    async Task Because() => _exception = await Catch.Exception(() => _readModels.GetInstanceById(typeof(MyReadModel), _key));

    [Fact] void should_throw_unknown_read_model() => _exception.ShouldBeOfExactType<UnknownReadModel>();
}
