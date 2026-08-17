// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_renaming_read_model.original;

public class BoardProjection : IProjectionFor<Board>
{
    public void Define(IProjectionBuilderFor<Board> builder) => builder
        .From<BoardNamed>(b => b.Set(m => m.Name).To(e => e.Name));
}
