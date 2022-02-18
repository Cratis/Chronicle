// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Events.Applications;

namespace Read.Applications
{
    public class ApplicationResourcesProjection : IProjectionFor<ApplicationResources>
    {
        public ProjectionId Identifier => "6390f7ab-67c1-4b18-ac61-79b92c8a8352";

        public void Define(IProjectionBuilderFor<ApplicationResources> builder) => builder
            .From<MongoDBConnectionStringChangedForApplication>(_ => _
                .Set(m => m.MongoDB.ConnectionString).To(e => e.ConnectionString))
            .Children(m => m.MongoDB.Users, cb => cb
                .IdentifiedBy(m => m.User)
                .From<MongoDBUserChanged>(e => e
                    .UsingKey(e => e.User)
                    .Set(m => m.User).To(e => e.User)
                    .Set(m => m.Password).To(e => e.Password)))
            .From<IpAddressSetForApplication>(_ => _
                .Set(m => m.IpAddress).To(e => e.Address));
    }
}
