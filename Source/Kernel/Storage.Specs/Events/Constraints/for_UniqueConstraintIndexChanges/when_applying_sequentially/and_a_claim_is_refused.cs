// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.Events.Constraints.for_UniqueConstraintIndexChanges.when_applying_sequentially;

public class and_a_claim_is_refused : Specification
{
    IUniqueConstraintsStorage _storage;
    UniqueConstraintDefinition _definition;
    UniqueConstraintIndexChange[] _changes;
    IReadOnlyCollection<int> _refused;

    void Establish()
    {
        _storage = Substitute.For<IUniqueConstraintsStorage>();
        _definition = new("names", []);
        _changes =
        [
            new(UniqueConstraintIndexChangeKind.Save, "first", 1, "scope", "taken"),
            new(UniqueConstraintIndexChangeKind.Save, "second", 2, "scope", "taken"),
            new(UniqueConstraintIndexChangeKind.Remove, "third", 3, "scope"),
            new(UniqueConstraintIndexChangeKind.RemoveValue, "fourth", 4, "scope", "released")
        ];
        _storage.Save("second", _definition, 2, "taken", "scope", Arg.Any<Subject?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new DuplicateUniqueConstraintValue(_definition.Name, "second")));
    }

    async Task Because() => _refused = await UniqueConstraintIndexChanges.ApplySequentially(_storage, _definition, _changes);

    [Fact] void should_report_the_refused_claim_by_its_position() => _refused.ShouldContainOnly(1);
    [Fact] async Task should_apply_the_first_claim() => await _storage.Received(1).Save("first", _definition, 1, "taken", "scope", null, Arg.Any<CancellationToken>());
    [Fact] async Task should_go_on_to_the_removal_after_the_refused_claim() => await _storage.Received(1).Remove("third", _definition, "scope", Arg.Any<CancellationToken>());
    [Fact] async Task should_go_on_to_release_the_single_value() => await _storage.Received(1).RemoveValue("fourth", _definition, "released", "scope", Arg.Any<CancellationToken>());
}
