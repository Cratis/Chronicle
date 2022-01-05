// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events.Projections;
using Cratis.Workbench.Compliance.Events.Microservices;

namespace Cratis.Workbench.Compliance.Read.Microservices
{
    [Projection("afcdf3df-53ab-4c35-94ab-07be4500b2ec")]
    public class MicroservicesProjection : IProjectionFor<Microservice>
    {
        public void Define(IProjectionBuilderFor<Microservice> builder) => builder
            .From<MicroserviceAdded>(_ => _
                .Set(m => m.Name).To(e => e.Name));
    }
}
