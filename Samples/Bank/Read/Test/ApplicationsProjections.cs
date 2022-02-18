// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Events.Applications;

namespace Read.Applications
{
    public class ApplicationsProjections : IProjectionFor<Application>
    {
        public ProjectionId Identifier => "5a52002b-f36e-4d69-8900-cd133de4aac3";

        public void Define(IProjectionBuilderFor<Application> builder) => builder
            .From<ApplicationCreated>(_ => _
                .Set(m => m.Name).To(e => e.Name)
                .Set(m => m.CloudLocation).To(e => e.CloudLocation))
            .Children(_ => _.Microservices, _ => _
                .IdentifiedBy(m => m.MicroserviceId)
                .From<MicroserviceCreated>(_ => _
                    .UsingParentKey(e => e.ApplicationId)
                    .Set(m => m.Name).To(e => e.Name))
                .Children(_ => _.Deployables, _ => _
                    .IdentifiedBy(m => m.DeployableId)
                    .From<DeployableCreated>(_ => _
                        .UsingParentKey(e => e.MicroserviceId)
                        .Set(m => m.Name).To(e => e.Name))
                    .Children(_ => _.Users, _ => _
                        .IdentifiedBy(m => m.User)
                        .From<MongoDBUserChanged>(_ => _
                            .Set(m => m.User).To(e => e.User)
                            .Set(m => m.Password).To(e => e.Password)))))
            .RemovedWith<ApplicationRemoved>();
    }
}
