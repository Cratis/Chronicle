// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Namespaces.for_NamespaceExceptionCodes;

public class when_an_aggregate_has_conflicting_failures : Specification
{
    string _code;

    void Because() => _code = NamespaceExceptionCodes.For(new AggregateException(new NamespaceClosing("tenant"), new NamespaceRecoveryRequired("tenant")))!;

    [Fact] void should_not_choose_one_ambiguous_outcome() => _code.ShouldBeNull();
}
