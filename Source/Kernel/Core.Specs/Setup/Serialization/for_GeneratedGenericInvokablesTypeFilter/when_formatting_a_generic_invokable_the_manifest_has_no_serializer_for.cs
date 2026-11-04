// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Setup.Serialization.for_GeneratedGenericInvokablesTypeFilter;

public class when_formatting_a_generic_invokable_the_manifest_has_no_serializer_for : given.a_kernel_type_allow_list
{
    bool? _verdict;
    Exception _error;

    void Because()
    {
        _verdict = _filter.IsTypeAllowed(typeof(Request<>));
        _error = Catch.Exception(() => _converter.Format(typeof(Request<string>)));
    }

    [Fact] void should_not_decide_for_the_definition() => _verdict.ShouldBeNull();
    [Fact] void should_refuse_the_request_type() => _error.ShouldBeOfExactType<InvalidOperationException>();
}
