// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Observation.Reducers;

namespace Aksio.Cratis.Reducers.for_ReducerMethodInfoExtensions.when_asking_is_reducer_method.with_nullable_disabled;

public class and_signature_is_a_valid_synchronous_method_without_context_with_wrong_read_model_as_return_type : Specification
{
    class MyReducer : IReducerFor<ReadModel>
    {
        public ReducerId Id => "55753433-5bbd-4a79-91b6-7b6231c52183";

        public object Something(ValidEvent @event, ReadModel current) => null!;
    }

    bool result;

    void Because() => result = typeof(MyReducer).GetMethod(nameof(MyReducer.Something)).IsReducerMethod(typeof(ReadModel), Enumerable.Empty<Type>());

    [Fact] void should_not_be_considered_a_reducer_method() => result.ShouldBeFalse();
}
