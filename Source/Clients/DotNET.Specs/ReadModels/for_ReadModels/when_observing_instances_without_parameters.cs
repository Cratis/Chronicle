// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModels;

#pragma warning disable CA2263 // Prefer generic overload when type is known
public class when_observing_instances_without_parameters : given.all_dependencies
{
    class MyReadModel
    {
        public string Name { get; set; } = string.Empty;
    }

    IObservable<IEnumerable<MyReadModel>> _result = null!;
    Contracts.ReadModels.IMaterializedReadModels _materializedReadModelsService = null!;

    void Establish()
    {
        _projections.HasFor(typeof(MyReadModel)).Returns(true);
        _reducers.HasFor(typeof(MyReadModel)).Returns(false);

        _materializedReadModelsService = Substitute.For<Contracts.ReadModels.IMaterializedReadModels>();
        _services.MaterializedReadModels.Returns(_materializedReadModelsService);
    }

    void Because() => _result = _readModels.Materialized.ObserveInstances<MyReadModel>();

    [Fact] void should_return_observable() => _result.ShouldNotBeNull();
}
#pragma warning restore CA2263 // Prefer generic overload when type is known
