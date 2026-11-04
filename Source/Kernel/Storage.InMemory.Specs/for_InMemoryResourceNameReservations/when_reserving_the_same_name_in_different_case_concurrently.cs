// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Naming;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.InMemory.for_InMemoryResourceNameReservations;

public class when_reserving_the_same_name_in_different_case_concurrently : Specification
{
    List<Task> _creations;
    int _created;
    int _refused;

    void Establish()
    {
        var reservations = new InMemoryResourceNameReservations();
        var gate = new ResourceNameGate(new ResourceNameSnapshot(), reservations, ResourceNameExemptions.None, NullLogger<ResourceNameGate>.Instance);
        _creations = [];
        foreach (var name in new[] { "Orders", "orders", "ORDERS", "oRders" })
        {
            var creation = new ResourceCreation(new EventStoreName(name), null, []);
            _creations.Add(Task.Run(async () =>
            {
                try
                {
                    await gate.EnsureCanCreate(creation);
                    Interlocked.Increment(ref _created);
                }
                catch (ResourceNameConflict)
                {
                    Interlocked.Increment(ref _refused);
                }
            }));
        }
    }

    void Because() => Task.WaitAll([.. _creations]);

    [Fact] void should_create_exactly_one() => _created.ShouldEqual(1);
    [Fact] void should_refuse_the_others() => _refused.ShouldEqual(3);
}
