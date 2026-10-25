// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;
using Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_ObserverIdentityClaimsExtensions.given;

public abstract class a_binding_to_allocate_for : Specification
{
    protected IObserverIdentityClaims _claims;
    protected EventStoreName _store = "claims";
    protected MaterializationRevision _revision = "revision";
    protected MaterializationBinding _binding;
    protected MaterializationGeneration _result;

    void Establish()
    {
        _claims = Substitute.For<IObserverIdentityClaims>();
        _claims.Claim(Arg.Any<EventStoreName>(), Arg.Any<ObserverIdentityClaim>()).Returns(call => Task.FromResult(call.ArgAt<ObserverIdentityClaim>(1)));
        var key = new MaterializationKey(new(Guid.Parse("fa5dc415-e617-48a0-9975-fd1580aa321a")), "one", "projection", "mongodb", SinkConfigurationId.None);
        _binding = new(key, "models", MaterializationGeneration.First, "models", ServingEpoch.Initial, MaterializationGeneration.First, null, null, []);
    }

    protected ObserverId IdOf(uint number) => MaterializationGenerationIdentity.For(_binding.Key.Projection, new(number), _binding.Container).ObserverId;

    protected ObserverIdentityClaim Claim(uint number, MaterializationRevision revision) =>
        new(IdOf(number), ObserverIdentityClaimant.Generation, _binding.Key.Projection, new(number), revision);

    protected void Holds(uint number, ObserverIdentityClaim claim) => _claims.Get(_store, IdOf(number)).Returns(Task.FromResult<ObserverIdentityClaim?>(claim));
}
