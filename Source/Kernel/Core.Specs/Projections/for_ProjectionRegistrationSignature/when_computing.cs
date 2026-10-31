// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.for_ProjectionRegistrationSignature;

public class when_computing : a_registration
{
    string _original;
    string _definitionsReordered;
    string _dictionaryEntriesReordered;
    string _withAnotherLastUpdated;
    string _computedAgain;

    void Because()
    {
        var first = Definition("a");
        var second = Definition("b");
        _original = SignatureOf(first, second);
        _computedAgain = SignatureOf(Definition("a"), Definition("b"));
        _definitionsReordered = SignatureOf(second, first);

        var reversed = first with
        {
            From = new Dictionary<EventType, FromDefinition>
            {
                [SecondEvent] = first.From[SecondEvent],
                [FirstEvent] = first.From[FirstEvent]
            }
        };
        _dictionaryEntriesReordered = SignatureOf(reversed, second);
        _withAnotherLastUpdated = SignatureOf(first with { LastUpdated = DateTimeOffset.UtcNow }, second);
    }

    [Fact] void should_be_the_same_for_the_same_content() => _computedAgain.ShouldEqual(_original);
    [Fact] void should_not_depend_on_the_order_the_definitions_were_submitted_in() => _definitionsReordered.ShouldEqual(_original);
    [Fact] void should_not_depend_on_the_order_of_dictionary_entries() => _dictionaryEntriesReordered.ShouldEqual(_original);
    [Fact] void should_not_depend_on_when_the_definition_was_last_updated() => _withAnotherLastUpdated.ShouldEqual(_original);
}
