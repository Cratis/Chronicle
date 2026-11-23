// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Sequences.for_AppendManyToNamespace;

public class when_validating : given.a_protected_batch_endpoint
{
    readonly AppendManyToNamespaceValidator _validator = new();

    [Fact] void should_accept_the_complete_batch() => _validator.Validate(_command).IsValid.ShouldBeTrue();
    [Fact] void should_require_the_identity() => _validator.Validate(_command with { ExpectedIncarnation = NamespaceIncarnationId.NotSet }).IsValid.ShouldBeFalse();
    [Fact] void should_require_the_source() => _validator.Validate(_command with { EventSourceId = "" }).IsValid.ShouldBeFalse();
    [Fact] void should_require_events() => _validator.Validate(_command with { Events = [] }).IsValid.ShouldBeFalse();
    [Fact] void should_validate_named_tags() => _validator.Validate(_command with { Events = [new(new("event", 1, false), "{}", [new("", "one")])] }).IsValid.ShouldBeFalse();
}
