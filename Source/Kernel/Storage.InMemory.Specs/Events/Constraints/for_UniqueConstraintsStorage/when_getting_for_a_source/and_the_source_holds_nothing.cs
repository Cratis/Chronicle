// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorage.when_getting_for_a_source;

public class and_the_source_holds_nothing : given.a_unique_constraints_storage
{
    IEnumerable<UniqueConstraintValue> _values;

    Task Establish() => _storage.Save("other", _definition, 1, "held");
    async Task Because() => _values = await _storage.GetFor("absent", _definition);

    [Fact] void should_return_no_values_for_the_absent_source() => _values.ShouldBeEmpty();
}
