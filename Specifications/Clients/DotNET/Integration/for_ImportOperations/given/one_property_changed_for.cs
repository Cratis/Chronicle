// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Changes;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Integration.for_ImportOperations.given;

public class one_property_changed_for<TEvent> : all_dependencies_for<TEvent>
{
    protected Model initial;
    protected Model mapped;
    protected ExternalModel incoming;
    protected ImportOperations<Model, ExternalModel> operations;

    void Establish()
    {
        initial = new(42, "Forty Two", "Two");
        incoming = new(43, "Forty Two");
        mapped = new(incoming.SomeInteger, incoming.SomeString, null);

        projection.Setup(_ => _.GetById(key)).Returns(Task.FromResult(new AdapterProjectionResult<Model>(initial, Array.Empty<PropertyPath>(), 0)));
        mapper.Setup(_ => _.Map<Model>(incoming)).Returns(mapped);

        objects_comparer = new();
        objects_comparer
            .Setup(_ => _.Equals(initial, IsAny<Model>(), out Ref<IEnumerable<PropertyDifference>>.IsAny))
            .Returns((object? _, object? __, out IEnumerable<PropertyDifference> differences) =>
            {
                differences = new[]
                {
                        new PropertyDifference(new(nameof(Model.SomeInteger)), initial.SomeInteger, incoming.SomeInteger)
                };
                return false;
            });

        operations = new(
            adapter.Object,
            projection.Object,
            mapper.Object,
            objects_comparer.Object,
            event_log.Object,
            event_outbox.Object,
            causation_manager.Object);
    }
}
