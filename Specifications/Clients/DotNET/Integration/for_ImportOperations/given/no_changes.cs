// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Integration.for_ImportOperations.given
{
    public class no_changes : all_dependencies
    {
        protected Model initial;
        protected ExternalModel incoming;
        protected ImportOperations<Model, ExternalModel> operations;

        void Establish()
        {
            initial = new(42, "Forty Two");
            incoming = new(42, "Forty Two");

            projection.Setup(_ => _.GetById(key)).Returns(initial);
            mapper.Setup(_ => _.Map<Model>(incoming)).Returns(initial);

            operations = new(
                adapter.Object,
                projection.Object,
                mapper.Object,
                event_log.Object
            );
        }
    }
}
