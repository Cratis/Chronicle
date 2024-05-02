// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.MongoDB.Resilience.for_MongoCollectionInterceptorForReturnValue;

public class InvocationTarget
{
    public const string ErrorMessage = "Something went wrong";

    public Task<string> SuccessfulMethod() => Task.FromResult("Hello");
    public Task<string> CancelledMethod() => Task.FromCanceled<string>(new CancellationToken(true));
    public Task<string> FaultedMethod() => Task.Run<string>(() =>
    {
        #pragma warning disable AS0008, CA2201
        throw new(ErrorMessage);
#pragma warning disable CS0162 // Unreachable code detected
        return "Hello";
#pragma warning restore CS0162 // Unreachable code detected
    });

    public Task<string> FaultedMethodWithoutTask() => throw new(ErrorMessage);
}
