// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.MongoDB.Resilience.for_MongoCollectionInterceptor.when_intercepting;

public class successful_method : given.an_interceptor
{
    protected override string GetInvocationTargetMethod() => nameof(InvocationTarget.SuccessfulMethod);

    void Because() => interceptor.Intercept(invocation.Object);

    [Fact] void should_return_successful_task() => return_value.IsCompletedSuccessfully.ShouldBeTrue();
    [Fact] void should_have_freed_up_semaphore() => semaphore.CurrentCount.ShouldEqual(pool_size);
}
