// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintIndexUpdater.when_updating;

public class and_mode_is_per_value_and_constrained_properties_have_no_value : Specification
{
    IUniqueConstraintsStorage _storage;
    UniqueConstraintIndexUpdater _updater;

    void Establish()
    {
        _storage = Substitute.For<IUniqueConstraintsStorage>();
        var definition = new UniqueConstraintDefinition("SomeConstraint", [new("SomeEvent", ["SomeProperty"])]) { Mode = UniqueConstraintMode.PerValue };
        var context = new ConstraintValidationContext([], EventSourceId.New(), "SomeEvent", new ExpandoObject());
        _updater = new(definition, context, _storage);
    }

    async Task Because() => await _updater.Update(42L);

    [Fact] void should_keep_existing_claims() => _storage.ReceivedCalls().Any(_ => _.GetMethodInfo().Name == nameof(IUniqueConstraintsStorage.Remove) || _.GetMethodInfo().Name == nameof(IUniqueConstraintsStorage.RemoveValue)).ShouldBeFalse();
    [Fact] void should_not_claim_an_empty_value() => _storage.ReceivedCalls().Any(_ => _.GetMethodInfo().Name == nameof(IUniqueConstraintsStorage.Save)).ShouldBeFalse();
}
