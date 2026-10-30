// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexBatches.when_collecting;

public class and_the_storage_cannot_apply_changes_together : Specification
{
    ConstraintIndexBatches _batches;
    bool _taken;

    void Because()
    {
        var validator = new UniqueConstraintValidator(new UniqueConstraintDefinition("names", [new("Claimed", ["name"])]), Substitute.For<IUniqueConstraintsStorage>());
        var content = new ExpandoObject();
        ((IDictionary<string, object?>)content)["name"] = "a name";
        var context = new ConstraintValidationContext([validator], "first", "Claimed", content);
        _batches = new();
        _taken = _batches.TryAdd(validator, context.IndexUpdates.Single().Updater, 1);
    }

    [Fact] void should_leave_the_update_to_be_made_on_its_own() => _taken.ShouldBeFalse();
    [Fact] void should_collect_nothing() => _batches.Batches.ShouldBeEmpty();
}
