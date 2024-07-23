// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration;
using Cratis.Chronicle.Rules;

namespace Cratis.Chronicle.Projections.for_ClientProjections.given;

public class all_dependencies : Specification
{
    protected Mock<IProjections> projections;
    protected Mock<IAdapters> adapters;
    protected Mock<IRulesProjections> rules_projections;

    void Establish()
    {
        projections = new();
        adapters = new();
        rules_projections = new();
    }
}
