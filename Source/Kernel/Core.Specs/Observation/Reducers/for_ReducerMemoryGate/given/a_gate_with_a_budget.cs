// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Memory;
using Cratis.Chronicle.Properties;
using Microsoft.Extensions.Options;

#pragma warning disable CA2012 // Verifying a substituted call does not use the value task it returns.

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerMemoryGate.given;

public class a_gate_with_a_budget : Specification
{
    protected const string Scope = "store/namespace/read-model";

    protected IMemoryBudget _budget;
    protected ReducerMemoryGate _gate;
    protected Key _partition;

    protected virtual MemoryManagement Settings => new() { MinimumReservationBytes = 1000, ReducerCostMultiplier = 4 };

    void Establish()
    {
        _budget = Substitute.For<IMemoryBudget>();
        _budget.Reserve(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(callInfo => ValueTask.FromResult(MemoryReservation.None));
        _gate = new(Options.Create(new ChronicleOptions { Memory = Settings }), _budget);
        _partition = new Key("partition", ArrayIndexers.NoIndexers);
    }

    protected static ExpandoObject StateOfAboutBytes(int characters)
    {
        var state = new ExpandoObject();
        ((IDictionary<string, object?>)state)["text"] = new string('x', characters);
        return state;
    }
}
