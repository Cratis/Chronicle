// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Kernel.given;

namespace Cratis.Chronicle.Projections.Kernel.for_KernelProjectionPropertyPathResolver.when_resolving;

public class and_the_accessor_does_not_name_a_property : Specification
{
    Exception _result;

    void Because() => _result = Catch.Exception(() =>
        KernelProjectionPropertyPathResolver.Resolve<a_read_model, string>(_ => "not a property"));

    [Fact] void should_fail_rather_than_resolve_to_an_empty_path() =>
        _result.ShouldBeOfExactType<InvalidKernelProjectionPropertyAccessor>();
}
