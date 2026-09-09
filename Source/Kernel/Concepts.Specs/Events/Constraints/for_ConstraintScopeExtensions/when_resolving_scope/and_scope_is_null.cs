// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Concepts.Specs.Events.Constraints.for_ConstraintScopeExtensions.when_resolving_scope;

public class and_scope_is_null : Specification
{
    ResolvedConstraintScope? _result;

    void Because() => _result = ((ConstraintScope?)null).ResolveFor("SourceType", "StreamType", "StreamId");

    [Fact] void should_not_narrow_anything() => _result.ShouldBeNull();
}
