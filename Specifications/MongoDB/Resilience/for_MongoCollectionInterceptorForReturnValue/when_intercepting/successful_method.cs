// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.MongoDB.Resilience.for_MongoCollectionInterceptorForReturnValue.when_intercepting;

public class successful_method : given.an_interceptor
{
    string result;
    protected override string GetInvocationTargetMethod() => nameof(InvocationTarget.SuccessfulMethod);

    async Task Because()
    {
        interceptor.Intercept(invocation.Object);
        result = await return_value;
    }

    [Fact] void should_return_value_from_invocation() => result.ShouldEqual("Hello");
    [Fact] void should_return_successful_task() => return_value.IsCompletedSuccessfully.ShouldBeTrue();
    [Fact] void should_have_freed_up_semaphore() => semaphore.CurrentCount.ShouldEqual(pool_size);
}
