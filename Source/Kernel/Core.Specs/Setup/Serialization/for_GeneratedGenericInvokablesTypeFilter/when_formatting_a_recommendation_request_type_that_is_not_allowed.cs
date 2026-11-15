// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Recommendations;
using ForeignAssemblies.Recommendations;

namespace Cratis.Chronicle.Setup.Serialization.for_GeneratedGenericInvokablesTypeFilter;

public class when_formatting_a_recommendation_request_type_that_is_not_allowed : given.a_kernel_type_allow_list
{
    Type _invokable;
    Exception _error;

    void Establish() => _invokable = typeof(IRecommendation).Assembly.GetTypes().First(type =>
        type.IsGenericTypeDefinition &&
        type.Name.StartsWith("Invokable_IRecommendation_", StringComparison.Ordinal) &&
        type.GetGenericArguments().Length == 1);

    void Because() => _error = Catch.Exception(() => _converter.Format(_invokable.MakeGenericType(typeof(ForeignRequest))));

    [Fact] void should_allow_the_generated_invokable_definition() => _filter.IsTypeAllowed(_invokable).ShouldEqual(true);
    [Fact] void should_refuse_the_request_type() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_name_the_type_that_is_not_allowed() => _error.Message.ShouldContain(nameof(ForeignRequest));
}
