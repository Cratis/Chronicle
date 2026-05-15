// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_ReadModels;

/// <summary>
/// Projection for <see cref="PiiReadModel"/> used in integration tests.
/// </summary>
public class PiiProjection : IProjectionFor<PiiReadModel>
{
    /// <summary>
    /// Gets the projection identifier.
    /// </summary>
    public ProjectionId Identifier => "pii-projection";

    /// <summary>
    /// Defines the projection.
    /// </summary>
    /// <param name="builder">The projection builder.</param>
    public void Define(IProjectionBuilderFor<PiiReadModel> builder) => builder
        .From<PiiEvent>(e => e
            .Set(m => m.Name).To(e => e.Name)
            .Set(m => m.SocialSecurityNumber).To(e => e.SocialSecurityNumber));
}
