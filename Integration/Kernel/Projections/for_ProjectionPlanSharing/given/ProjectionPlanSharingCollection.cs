// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Kernel.Integration.Projections.for_ProjectionPlanSharing.given;

[CollectionDefinition(Name)]
public class ProjectionPlanSharingCollection : ICollectionFixture<ProjectionPlanSharingFixture>
{
    public const string Name = "Projection plan sharing";
}
