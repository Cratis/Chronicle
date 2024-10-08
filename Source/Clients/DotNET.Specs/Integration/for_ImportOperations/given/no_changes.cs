// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Integration.for_ImportOperations.given;

public class no_changes : all_dependencies_for<SomeEvent>
{
    protected Model initial;
    protected ExternalModel incoming;
    protected ImportOperations<Model, ExternalModel> operations;

    void Establish()
    {
        initial = new(42, "Forty Two", "Two");
        incoming = new(42, "Forty Two");

        projection.Setup(_ => _.GetById(key)).Returns(Task.FromResult(new AdapterProjectionResult<Model>(initial, [], 0)));
        mapper.Setup(_ => _.Map<Model>(incoming)).Returns(initial);
        objects_comparer.Setup(_ => _.Compare(initial, IsAny<Model>(), out Ref<IEnumerable<PropertyDifference>>.IsAny)).Returns(true);

        operations = new(
            adapter.Object,
            projection.Object,
            mapper.Object,
            objects_comparer.Object,
            event_log.Object,
            causation_manager.Object);
    }
}
