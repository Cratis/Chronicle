// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Testing.ReadModels.for_DecisionReadsForTesting;

public class StreamDecisionProjection : IProjectionFor<StreamDecisionState>
{
    public void Define(IProjectionBuilderFor<StreamDecisionState> builder) =>
        builder.Passive().From<ModuleCreated>(_ => _.UsingKeyFromContext(context => context.EventStreamId));
}
