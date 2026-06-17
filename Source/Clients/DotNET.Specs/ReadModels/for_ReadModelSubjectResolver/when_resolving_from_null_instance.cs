// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelSubjectResolver;

public class when_resolving_from_null_instance : Specification
{
    Subject? _result;

    void Because() => _result = ReadModelSubjectResolver.ResolveFrom(null);

    [Fact] void should_return_null() => _result.ShouldBeNull();
}
