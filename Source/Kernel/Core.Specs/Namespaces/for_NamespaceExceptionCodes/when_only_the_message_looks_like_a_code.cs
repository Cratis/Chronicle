// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Namespaces.for_NamespaceExceptionCodes;

public class when_only_the_message_looks_like_a_code : Specification
{
    string _code;

    void Because() => _code = NamespaceExceptionCodes.For(new Exception("NamespaceRecoveryRequired"))!;

    [Fact] void should_not_parse_exception_text() => _code.ShouldBeNull();
}
