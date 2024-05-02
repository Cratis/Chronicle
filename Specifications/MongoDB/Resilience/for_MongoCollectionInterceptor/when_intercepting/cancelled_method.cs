// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.MongoDB.Resilience.for_MongoCollectionInterceptor.when_intercepting;

public class cancelled_method : given.an_interceptor
{
    protected override string GetInvocationTargetMethod() => nameof(InvocationTarget.CancelledMethod);
    Exception exception;

    async Task Because()
    {
        interceptor.Intercept(invocation.Object);
        exception = await Catch.Exception(async () => await return_value);
    }

    [Fact] void should_bubble_up_cancelled_exception() => exception.ShouldBeOfExactType<TaskCanceledException>();
    [Fact] void should_have_cancelled_task() => return_value.IsCanceled.ShouldBeTrue();
    [Fact] void should_have_freed_up_semaphore() => semaphore.CurrentCount.ShouldEqual(pool_size);
}
