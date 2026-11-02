// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Placement.for_EventSequencePlacementDirector.given;

public class a_director : Cluster.given.a_role_placement
{
    protected EventSequencePlacementDirector _director;

    void Establish()
    {
        _director = new(_eligibility);
    }
}
