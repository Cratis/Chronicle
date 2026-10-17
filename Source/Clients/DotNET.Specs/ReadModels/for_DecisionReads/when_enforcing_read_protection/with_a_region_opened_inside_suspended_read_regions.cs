// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.ReadModels.for_DecisionReads.when_enforcing_read_protection;

public class with_a_region_opened_inside_suspended_read_regions : given.a_protected_reader
{
    Exception _innerRegionError;
    Exception _innerSuspensionError;
    Exception _afterInnerRegionError;

    async Task Because()
    {
        using var frame = _unit.EnforceReadProtection();
        using var outer = _unit.AllowReads(ReadAllowance.Advisory, "outer command body");
        using (_unit.SuspendReadRegions())
        {
            using (_unit.AllowReads(ReadAllowance.Advisory, "nested command body"))
            {
                _innerRegionError = await Catch.Exception(() => _reader.GetDetached<Model>("source"));
                using (_unit.SuspendReadRegions())
                {
                    _innerSuspensionError = await Catch.Exception(() => _reader.GetDetached<Model>("source"));
                }
            }
            _afterInnerRegionError = await Catch.Exception(() => _reader.GetDetached<Model>("source"));
        }
    }

    [Fact] void should_apply_a_region_opened_inside_the_suspension() => _innerRegionError.ShouldBeNull();
    [Fact] void should_suspend_again_when_nested() => _innerSuspensionError.ShouldBeOfExactType<DetachedReadWhileReadsAreProtected>();
    [Fact] void should_keep_the_outer_suspension_after_the_inner_region_closes() => _afterInnerRegionError.ShouldBeOfExactType<DetachedReadWhileReadsAreProtected>();
}
