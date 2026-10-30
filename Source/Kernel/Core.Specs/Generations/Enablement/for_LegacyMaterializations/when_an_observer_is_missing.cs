// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Generations.Enablement.for_LegacyMaterializations;

public class when_an_observer_is_missing : given.materializations
{
    Exception _error;

    void Establish() => _namespace.Observers.GetAll().Returns([]);

    async Task Because() => _error = await Catch.Exception(() => _reader.GetAll(new("orders")));

    [Fact] void should_not_invent_legacy_state() => _error.ShouldBeOfExactType<InvalidOperationException>();
}
