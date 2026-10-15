// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Reducers.for_Reducers;

public class when_reads_are_protected : given.all_dependencies
{
    Exception _instanceError;
    Exception _instancesError;

    async Task Because()
    {
        var unit = new UnitOfWork(CorrelationId.New(), _ => { }, _eventStore);
        using var frame = unit.EnforceReadProtection();
        _instanceError = await Catch.Exception(() => _reducers.GetInstanceById(typeof(object), "other-key"));
        _instancesError = await Catch.Exception(() => _reducers.GetInstances(typeof(object)));
    }

    [Fact] void should_refuse_instance_acquisition() => _instanceError.ShouldBeOfExactType<ReadRefusedWhileReadsAreProtected>();
    [Fact] void should_refuse_collection_acquisition() => _instancesError.ShouldBeOfExactType<ReadRefusedWhileReadsAreProtected>();
    [Fact] void should_refuse_before_transport() => _services.ReceivedCalls().ShouldBeEmpty();
}
