// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Namespaces.for_NamespaceExceptionCodes;

public class when_unwrapping_known_wrappers : Specification
{
    string _code;

    void Because() => _code = NamespaceExceptionCodes.For(new AggregateException(new TargetInvocationException(
        new Orleans.Runtime.OrleansException("private activation detail", new NamespaceRecoveryRequired("tenant")))))!;

    [Fact] void should_preserve_the_literal_failure() => _code.ShouldEqual("NamespaceRecoveryRequired");
    [Fact] void should_not_expose_wrapper_details() => _code.Contains("private", StringComparison.Ordinal).ShouldBeFalse();
}
