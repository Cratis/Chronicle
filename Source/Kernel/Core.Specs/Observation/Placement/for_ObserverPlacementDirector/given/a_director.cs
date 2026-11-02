// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Placement.for_ObserverPlacementDirector.given;

public class a_director : Cluster.given.a_role_placement
{
    protected ObserverPlacementDirector _director;

    void Establish()
    {
        _director = new(_eligibility);
    }
}
