// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.InMemory.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintIndexUpdater.when_updating;

public class and_the_write_is_cancelled : Specification
{
    UniqueConstraintsStorage _storage;
    UniqueConstraintDefinition _definition;
    UniqueConstraintIndexUpdater _updater;
    Exception _error;

    void Establish()
    {
        _storage = new();
        _definition = new("unique", [new("Claimed", ["email"])]);
        var content = new ExpandoObject();
        ((IDictionary<string, object?>)content)["email"] = "value";
        _updater = new(_definition, new([], "source", "Claimed", content), _storage);
    }

    async Task Because() => _error = await Catch.Exception(() => _updater.Update(0, new CancellationToken(canceled: true)));

    [Fact] void should_propagate_cancellation() => _error.ShouldBeOfExactType<OperationCanceledException>();
    [Fact] async Task should_not_write_the_claim() => (await _storage.GetFor("source", _definition)).ShouldBeEmpty();
}
