// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_Telemetry.when_replaying_with_default_orleans_tracing;

public class Projection : IProjectionFor<ReadModel>
{
    public void Define(IProjectionBuilderFor<ReadModel> builder) => builder.From<EventApplied>();
}
