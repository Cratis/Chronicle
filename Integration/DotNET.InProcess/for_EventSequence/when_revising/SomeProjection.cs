// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.InProcess.Integration.for_EventSequence.when_revising;

public class SomeProjection : IProjectionFor<SomeReadModel>
{
    public void Define(IProjectionBuilderFor<SomeReadModel> builder) => builder
        .From<SomeEvent>(e => e
            .Set(m => m.Content).To(ev => ev.Content))
        .From<AnotherEvent>(e => e
            .Set(m => m.Number).To(ev => ev.Number));
}
