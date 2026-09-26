// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Sequences.for_AppendManyForEventSources.when_validating;

public class and_the_only_scope_cannot_be_validated : Specification
{
    readonly CommandScenario<AppendManyForEventSources> _scenario = ChronicleCommandScenario.For<AppendManyForEventSources>();
    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        storage.HasEventStore(Arg.Any<EventStoreName>()).Returns(true);
        _scenario.Services.AddSingleton(storage);
    }

    [Theory]
    [InlineData("source", ulong.MaxValue, false)]
    [InlineData(" ", 0UL, false)]
    [InlineData("source", 0UL, true)]
    public async Task should_reject_an_uncheckable_scope(string label, ulong sequenceNumber, bool nullScope)
    {
        var scope = nullScope ? null! : new ConcurrencyScope(sequenceNumber, true);
        var result = await _scenario.Validate(new AppendManyForEventSources(
            "some-event-store",
            "some-namespace",
            "event-log",
            [],
            ConcurrencyScopes: [new EventSourceConcurrencyScope(label, scope)]));
        result.ShouldNotBeSuccessful();
        result.ShouldHaveValidationErrors();
    }
}
