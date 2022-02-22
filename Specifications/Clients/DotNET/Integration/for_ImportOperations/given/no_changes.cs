// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Changes;

namespace Aksio.Cratis.Integration.for_ImportOperations.given;

public class no_changes : all_dependencies
{
    protected Model initial;
    protected ExternalModel incoming;
    protected ImportOperations<Model, ExternalModel> operations;
    protected Mock<IObjectsComparer> objects_comparer;

    void Establish()
    {
        initial = new(42, "Forty Two");
        incoming = new(42, "Forty Two");

        projection.Setup(_ => _.GetById(key)).Returns(Task.FromResult(initial));
        mapper.Setup(_ => _.Map<Model>(incoming)).Returns(initial);

        objects_comparer = new();
        objects_comparer.Setup(_ => _.Equals(initial, IsAny<Model>(), out Ref<IEnumerable<PropertyDifference>>.IsAny)).Returns(true);

        operations = new(
            adapter.Object,
            projection.Object,
            mapper.Object,
            objects_comparer.Object,
            event_log.Object
        );
    }
}
