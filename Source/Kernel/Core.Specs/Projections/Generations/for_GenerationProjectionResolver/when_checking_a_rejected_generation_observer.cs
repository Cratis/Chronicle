// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Generations.Bindings;

namespace Cratis.Chronicle.Projections.Generations.for_GenerationProjectionResolver;

public class when_checking_a_rejected_generation_observer : given.a_generation_projection_resolver
{
    bool _result;
    GenerationExecution? _resolved;

    void Establish()
    {
        Bind(_binding);
        var claims = Substitute.For<IObserverIdentityClaims>();
        claims.Get(Store, _binding.Identity.ObserverId).Returns(ObserverIdentityClaim.ForRegistration(_binding.Identity.ObserverId));
        _resolver = new(_enablement, _builds, new ForwardingWriterGrants(_grants), _keys, _bindings, _projectionFactory, _specifications, claims);
    }

    async Task Because()
    {
        _result = await _resolver.IsGenerationObserver(Store, GenerationExecutions.Namespace, EventSequenceId.Log, _binding.Identity.ObserverId);
        _resolved = await Resolve(_binding.Identity.ObserverId);
    }

    [Fact] void should_not_identify_a_generation_observer() => _result.ShouldBeFalse();
    [Fact] void should_not_resolve_an_execution() => _resolved.ShouldBeNull();
}
