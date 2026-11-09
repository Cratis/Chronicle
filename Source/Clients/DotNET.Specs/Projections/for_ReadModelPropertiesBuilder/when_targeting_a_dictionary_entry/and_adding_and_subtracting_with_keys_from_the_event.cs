// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_ReadModelPropertiesBuilder.when_targeting_a_dictionary_entry;

public class and_adding_and_subtracting_with_keys_from_the_event : given.a_from_builder_with_dictionaries
{
    void Because()
    {
        _builder
            .Add(m => m.Totals, e => e.Category).With(e => e.Amount)
            .Subtract(m => m.Totals, e => e.Name).With(e => e.Amount);
        _result = _builder.Build();
    }

    [Fact] void should_add_to_the_entry_keyed_by_the_category() => _result.Properties["Totals.$eventContent.Category"].ShouldEqual("$add(Amount)");
    [Fact] void should_subtract_from_the_entry_keyed_by_the_name() => _result.Properties["Totals.$eventContent.Name"].ShouldEqual("$subtract(Amount)");
}
