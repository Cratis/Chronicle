// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.InProcess.Integration.for_EventSequence.when_appending_events_with_multiple_generation_migrations;

/// <summary>
/// Projection that consumes generation 2 of UserRegistered using auto-mapping.
/// </summary>
public class UserRegisteredProjection : IProjectionFor<UserReadModel>
{
    public void Define(IProjectionBuilderFor<UserReadModel> builder) => builder
        .AutoMap()
        .From<UserRegisteredV2>();
}
