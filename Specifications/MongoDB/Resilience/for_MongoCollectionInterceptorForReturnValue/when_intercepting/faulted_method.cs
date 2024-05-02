// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.MongoDB.Resilience.for_MongoCollectionInterceptorForReturnValue.when_intercepting;

public class faulted_method : given.an_interceptor
{
    Exception exception;

    protected override string GetInvocationTargetMethod() => nameof(InvocationTarget.FaultedMethod);

    async Task Because()
    {
        interceptor.Intercept(invocation.Object);
        exception = await Catch.Exception(async () => await return_value);
    }

    [Fact] void should_be_faulted() => return_value.IsFaulted.ShouldBeTrue();
    [Fact] void should_bubble_up_exception() => exception.Message.ShouldEqual(InvocationTarget.ErrorMessage);
    [Fact] void should_have_freed_up_semaphore() => semaphore.CurrentCount.ShouldEqual(pool_size);
}
