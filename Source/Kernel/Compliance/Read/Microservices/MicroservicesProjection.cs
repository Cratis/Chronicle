// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Compliance.Events.Microservices;
using Aksio.Cratis.Events.Projections;

namespace Aksio.Cratis.Compliance.Read.Microservices;

/// <summary>
/// Defines the projection for <see cref="Microservice"/>.
/// </summary>
public class MicroservicesProjection : IProjectionFor<Microservice>
{
    /// <inheritdoc/>
    public ProjectionId Identifier => "afcdf3df-53ab-4c35-94ab-07be4500b2ec";

    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<Microservice> builder) => builder
        .From<MicroserviceAdded>(_ => _
            .Set(m => m.Name).To(e => e.Name));
}
