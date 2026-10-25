// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Generations;

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_ObserverIdentityClaims.given;

public abstract class a_claims_storage<THarness> : Specification
    where THarness : IMaterializationBindingStorageHarness, new()
{
    protected EventStoreName _store = "claims";
    protected ObserverId _observerId = "projection@g2";
    protected IObserverIdentityClaims _claims;
    protected ObserverIdentityClaim _registration;
    protected ObserverIdentityClaim _generation;
    THarness _harness;

    void Establish()
    {
        _harness = CreateHarness();
        _claims = _harness.Claims;
        _registration = ObserverIdentityClaim.ForRegistration(_observerId);
        _generation = new(_observerId, ObserverIdentityClaimant.Generation, new ProjectionId("projection"), new MaterializationGeneration(2), new MaterializationRevision("revision"));
    }

    void Destroy() => _harness.Dispose();
    protected virtual THarness CreateHarness() => new();
}
