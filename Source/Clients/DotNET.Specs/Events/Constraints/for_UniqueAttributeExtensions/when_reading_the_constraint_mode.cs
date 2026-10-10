// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_UniqueAttributeExtensions;

public class when_reading_the_constraint_mode : Specification
{
    UniqueConstraintMode _perValue;
    UniqueConstraintMode _default;

    void Because()
    {
        _perValue = typeof(VersionRegistered).GetProperty(nameof(VersionRegistered.Id)).GetConstraintMode();
        _default = typeof(VersionRegistered).GetProperty(nameof(VersionRegistered.Name)).GetConstraintMode();
    }

    [Fact] void should_read_the_declared_mode() => _perValue.ShouldEqual(UniqueConstraintMode.PerValue);
    [Fact] void should_default_to_one_value_per_source() => _default.ShouldEqual(UniqueConstraintMode.PerEventSource);

    record VersionRegistered([property: Unique(Mode = UniqueConstraintMode.PerValue)] string Id, [property: Unique] string Name);
}
