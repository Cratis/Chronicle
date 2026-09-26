// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.given;

public static class PerRunArtifacts
{
    public static IClientArtifactsProvider WithEvent()
    {
        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.EventTypes.Returns([typeof(PerRunRecorded)]);
        return artifacts;
    }

    public static IClientArtifactsProvider WithProjection()
    {
        var artifacts = WithEvent();
        artifacts.Projections.Returns([typeof(PerRunProjection)]);
        return artifacts;
    }
}
