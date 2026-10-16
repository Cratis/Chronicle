// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.ReadModels.for_DecisionReads.when_enforcing_read_protection;

public class with_suspended_read_regions : given.a_protected_reader
{
    Exception _suspendedError;
    Exception _restoredError;

    async Task Because()
    {
        using var frame = _unit.EnforceReadProtection();
        using var advisory = _unit.AllowReads(ReadAllowance.Advisory, "outer command body");
        using (_unit.SuspendReadRegions())
        {
            _suspendedError = await Catch.Exception(() => _reader.GetDetached<Model>("source"));
        }
        _restoredError = await Catch.Exception(() => _reader.GetDetached<Model>("source"));
    }

    [Fact] void should_refuse_reads_the_inherited_region_would_allow() => _suspendedError.ShouldBeOfExactType<DetachedReadWhileReadsAreProtected>();
    [Fact] void should_restore_the_inherited_region_when_disposed() => _restoredError.ShouldBeNull();
}
