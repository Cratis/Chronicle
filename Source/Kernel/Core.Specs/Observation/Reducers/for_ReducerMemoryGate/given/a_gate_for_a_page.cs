// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

#pragma warning disable CA2012 // Verifying a substituted call does not use the value task it returns.

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerMemoryGate.given;

public class a_gate_for_a_page : a_gate_with_a_budget
{
    protected Key _first;
    protected Key _second;
    protected Key _neverReduced;

    void Establish()
    {
        _first = new Key("first", ArrayIndexers.NoIndexers);
        _second = new Key("second", ArrayIndexers.NoIndexers);
        _neverReduced = new Key("never-reduced", ArrayIndexers.NoIndexers);
    }
}
