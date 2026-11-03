// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Setup.Execution.for_SerializableFailureIncomingCallFilter;

public class when_a_call_fails : Specification
{
    SerializableFailureIncomingCallFilter _filter;

    void Establish() => _filter = new();

    async Task<Exception> Failing(Exception exception)
    {
        var context = Substitute.For<IIncomingGrainCallContext>();
        context.Invoke().Returns(Task.FromException(exception));
        var failure = await Catch.Exception(() => _filter.Invoke(context));
        return failure ?? throw new InvalidOperationException("The call did not fail");
    }

    [Fact] async Task should_replace_an_exception_from_a_foreign_namespace() => (await Failing(new Foreign.Driver.PoolExhaustedException("pool is full"))).ShouldBeOfExactType<InvalidOperationException>();

    [Fact] async Task should_name_the_original_type_and_keep_its_message()
    {
        var failure = await Failing(new Foreign.Driver.PoolExhaustedException("pool is full"));
        failure.Message.ShouldEqual("Foreign.Driver.PoolExhaustedException: pool is full");
    }

    [Fact] async Task should_replace_a_foreign_exception_wrapped_in_a_known_one()
    {
        var failure = await Failing(new TimeoutException("slow", new Foreign.Driver.PoolExhaustedException("pool is full")));
        failure.InnerException!.Message.ShouldEqual("Foreign.Driver.PoolExhaustedException: pool is full");
    }

    [Fact] async Task should_leave_a_known_exception_untouched()
    {
        var original = new TimeoutException("slow");
        (await Failing(original)).ShouldEqual(original);
    }
}
