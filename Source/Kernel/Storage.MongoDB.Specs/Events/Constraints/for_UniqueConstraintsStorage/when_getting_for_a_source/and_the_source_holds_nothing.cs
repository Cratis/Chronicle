// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.MongoDB.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.for_UniqueConstraintsStorage.when_getting_for_a_source;

[Collection(MongoDBCollection.Name)]
public class and_the_source_holds_nothing(MongoDBFixture fixture) : given.a_unique_constraints_storage(fixture)
{
    IEnumerable<UniqueConstraintValue> _values;

    Task Establish() => _storage.Save("other", _definition, 1, "held");
    async Task Because() => _values = await _storage.GetFor("absent", _definition);

    [Fact] void should_return_no_values_for_the_absent_source() => _values.ShouldBeEmpty();
}
