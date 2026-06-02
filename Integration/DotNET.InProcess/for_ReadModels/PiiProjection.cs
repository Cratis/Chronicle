// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.InProcess.Integration.for_ReadModels;

public class PiiProjection : IProjectionFor<PiiReadModel>
{
    public ProjectionId Identifier => "pii-projection";

    public void Define(IProjectionBuilderFor<PiiReadModel> builder) => builder
        .From<PiiEvent>(e => e
            .Set(m => m.Name).To(e => e.Name)
            .Set(m => m.SocialSecurityNumber).To(e => e.SocialSecurityNumber));
}
