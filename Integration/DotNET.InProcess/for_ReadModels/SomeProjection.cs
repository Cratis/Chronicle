// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.InProcess.Integration.for_ReadModels;

public class SomeProjection : IProjectionFor<SomeReadModel>
{
    public ProjectionId Identifier => "some-projection";

    public void Define(IProjectionBuilderFor<SomeReadModel> builder) => builder
        .From<SomeEvent>(e => e
            .Set(m => m.Number).To(e => e.Number))
        .From<AnotherEvent>(e => e
            .Set(m => m.Value).To(e => e.Value));
}
