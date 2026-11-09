// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Schema.for_VerifyMongoDBCapabilities;

public class when_canceled : given.a_verification
{
    async Task Because() => _error = await Catch.Exception(() => _task.Execute(new CancellationToken(true)));

    [Fact] void should_propagate_cancellation() => (_error is OperationCanceledException).ShouldBeTrue();
    [Fact] void should_not_probe() => _inspections.ShouldEqual(0);
}
