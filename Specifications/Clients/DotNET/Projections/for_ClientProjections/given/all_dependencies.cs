// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Integration;
using Aksio.Cratis.Rules;

namespace Aksio.Cratis.Projections.for_ClientProjections.given;

public class all_dependencies : Specification
{
    protected Mock<IProjections> projections;
    protected Mock<IImmediateProjections> immediate_projections;
    protected Mock<IAdapters> adapters;
    protected Mock<IRulesProjections> rules_projections;

    void Establish()
    {
        projections = new();
        immediate_projections = new();
        adapters = new();
        rules_projections = new();
    }
}
