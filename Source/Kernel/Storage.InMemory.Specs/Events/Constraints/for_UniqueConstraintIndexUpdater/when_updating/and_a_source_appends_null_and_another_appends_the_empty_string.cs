// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintIndexUpdater.when_updating;

/// <summary>
/// A missing value claims nothing, so it must not occupy the key the empty string hashes to. If it did, the
/// empty string appended by another event source would be rejected as a violation, or collide in the index.
/// </summary>
public class and_a_source_appends_null_and_another_appends_the_empty_string : given.a_unique_constraint_over_in_memory_storage
{
    ConstraintValidationResult _nullAppend;
    ConstraintValidationResult _emptyStringAppend;
    Exception? _error;

    async Task Because()
    {
        try
        {
            _nullAppend = await Append("source-a", null, 1);
            _emptyStringAppend = await Append("source-b", string.Empty, 2);
        }
        catch (Exception ex)
        {
            _error = ex;
        }
    }

    [Fact] void should_accept_the_null_append() => _nullAppend.IsValid.ShouldBeTrue();
    [Fact] void should_accept_the_empty_string_append() => _emptyStringAppend.IsValid.ShouldBeTrue();
    [Fact] void should_not_report_a_violation() => _emptyStringAppend.Violations.ShouldBeEmpty();
    [Fact] void should_not_fail_with_a_duplicate_unique_constraint_value() => (_error is DuplicateUniqueConstraintValue).ShouldBeFalse();
    [Fact] void should_not_fail_at_all() => _error.ShouldBeNull();
}
