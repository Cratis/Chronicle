// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas.for_CompensationSchemaProcessor;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator.when_generating_compensation_schemas;

public class and_the_marked_target_is_omitted : given.registry_bindings
{
    JsonSchema _precise;
    JsonSchema _legacy;
    JsonSchema _originalPrecise;
    JsonSchema _originalLegacy;
    JsonSchema _customPrecise;
    JsonSchema _customLegacy;
    CustomGenerator _custom;

    void Establish()
    {
        _originalPrecise = _generator.Generate(typeof(CompensatingEvent));
        _originalLegacy = _generator.GenerateLegacyEventType(typeof(CompensatingEvent));
        _custom = new();
    }

    void Because()
    {
        _precise = _generator.Generate(typeof(CompensatingEvent), _first);
        _legacy = _generator.GenerateLegacyEventType(typeof(CompensatingEvent), _first);
        _customPrecise = ((IJsonSchemaGenerator)_custom).Generate(typeof(CompensatingEvent), _first);
        _customLegacy = ((IJsonSchemaGenerator)_custom).GenerateLegacyEventType(typeof(CompensatingEvent), _first);
    }

    [Fact] void should_preserve_the_precise_cached_instance() => ReferenceEquals(_precise, _originalPrecise).ShouldBeTrue();
    [Fact] void should_preserve_the_legacy_cached_instance() => ReferenceEquals(_legacy, _originalLegacy).ShouldBeTrue();
    [Fact] void should_preserve_custom_precise_output_without_a_clone_or_rewrite() => ReferenceEquals(_customPrecise, _custom.Precise).ShouldBeTrue();
    [Fact] void should_preserve_custom_legacy_output_without_a_clone_or_rewrite() => ReferenceEquals(_customLegacy, _custom.Legacy).ShouldBeTrue();
    [Fact] void should_call_the_original_custom_precise_method() => _custom.PreciseCalls.ShouldEqual(1);
    [Fact] void should_call_the_original_custom_legacy_method() => _custom.LegacyCalls.ShouldEqual(1);
    [Fact] void should_not_resolve_the_omitted_target_through_the_registry() => _first.DidNotReceive().GetEventTypeFor(typeof(OriginalEvent));
}
