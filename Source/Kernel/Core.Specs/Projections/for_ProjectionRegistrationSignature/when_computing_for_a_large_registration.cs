// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.for_ProjectionRegistrationSignature;

/// <summary>
/// The signature is computed for every registration that arrives, so it has to cost far less than the work it saves.
/// Studio registers 171 definitions; the bound is generous so it only fails when the cost is of another order.
/// </summary>
public class when_computing_for_a_large_registration : a_registration
{
    const int DefinitionCount = 171;
    static readonly TimeSpan _bound = TimeSpan.FromSeconds(1);

    TimeSpan _elapsed;

    void Because()
    {
        var definitions = Enumerable.Range(0, DefinitionCount).Select(index =>
        {
            var definition = Definition($"the-projection-{index}");
            return definition with
            {
                From = Enumerable.Range(0, 20).ToDictionary(
                    eventIndex => new EventType($"e{eventIndex:D7}-1111-1111-1111-111111111111", 1),
                    eventIndex => new FromDefinition(Enumerable.Range(0, 10).ToDictionary(property => (PropertyPath)$"property{property}", property => $"property{property}"), PropertyExpression.NotSet, null))
            };
        }).ToArray();

        SignatureOf(definitions);
        var stopwatch = Stopwatch.StartNew();
        SignatureOf(definitions);
        _elapsed = stopwatch.Elapsed;
    }

    [Fact] void should_take_a_fraction_of_a_second() => _elapsed.ShouldBeLessThan(_bound);
}
