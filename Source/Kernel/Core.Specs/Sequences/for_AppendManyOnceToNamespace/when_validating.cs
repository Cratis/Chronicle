// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Sequences.for_AppendManyOnceToNamespace;

public class when_validating : given.a_namespace_bound_keyed_endpoint
{
    readonly AppendManyOnceToNamespaceValidator _validator = new();

    [Fact] void should_accept_the_mixed_batch() => _validator.Validate(_batch).IsValid.ShouldBeTrue();
    [Fact] void should_require_the_identity() => _validator.Validate(_batch with { ExpectedIncarnation = NamespaceIncarnationId.NotSet }).IsValid.ShouldBeFalse();
    [Fact] void should_require_events() => _validator.Validate(_batch with { Events = [] }).IsValid.ShouldBeFalse();
    [Fact] void should_validate_keys() => _validator.Validate(_batch with { Events = [_batch.Events.First() with { IdempotencyKey = "\n" }] }).IsValid.ShouldBeFalse();
    [Fact] void should_validate_named_tags() => _validator.Validate(_batch with { Events = [new(_batch.Events.First().Event with { NamedTags = [new("", "one")] }, "key")] }).IsValid.ShouldBeFalse();
}
