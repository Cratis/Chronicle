// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Changes;

namespace Aksio.Cratis.Integration.for_ImportOperations.given
{
    public class one_property_changed : all_dependencies
    {
        protected Model initial;
        protected Model mapped;
        protected ExternalModel incoming;
        protected ImportOperations<Model, ExternalModel> operations;
        protected Mock<IObjectsComparer> objects_comparer;

        void Establish()
        {
            initial = new(42, "Forty Two");
            incoming = new(43, "Forty Two");
            mapped = new(incoming.SomeInteger, incoming.SomeString);

            projection.Setup(_ => _.GetById(key)).Returns(Task.FromResult(initial));
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
                event_log.Object
            );
        }
    }
}
