// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Sequences.for_AppendOnceToNamespace;

public class when_validating : given.a_namespace_bound_keyed_endpoint
{
    readonly AppendOnceToNamespaceValidator _validator = new();

    [Fact] void should_accept_the_complete_append() => _validator.Validate(_single).IsValid.ShouldBeTrue();
    [Fact] void should_require_the_identity() => _validator.Validate(_single with { ExpectedIncarnation = NamespaceIncarnationId.NotSet }).IsValid.ShouldBeFalse();
    [Fact] void should_require_the_source() => _validator.Validate(_single with { EventSourceId = "" }).IsValid.ShouldBeFalse();
    [Fact] void should_require_the_key() => _validator.Validate(_single with { IdempotencyKey = "" }).IsValid.ShouldBeFalse();
    [Fact] void should_validate_named_tags() => _validator.Validate(_single with { NamedTags = [new("", "one")] }).IsValid.ShouldBeFalse();
}
