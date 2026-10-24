// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;
using Cratis.Chronicle.Generations.Enablement;
using Cratis.Chronicle.Storage.Generations.Bindings;

namespace Cratis.Chronicle.Generations.Bindings.for_GenerationCatalog.given;

public class a_catalog : Specification
{
    protected EventStoreName _store = "store";
    protected EventStoreNamespaceName _namespace = "tenant";
    protected MaterializationKey _key = new(new(Guid.NewGuid()), "tenant", "projection", "mongodb", new(Guid.NewGuid()));
    protected MaterializationGenerationRecord _record = new(MaterializationGenerationIdentity.For("projection", MaterializationGeneration.First, "models"), MaterializationGenerationState.Active, MaterializationRevision.Unknown, DateTimeOffset.UnixEpoch);
    protected IMaterializationKeys _keys;
    protected IMaterializationBindingStorage _storage;
    protected IGenerationCatalogSeam _catalog;
    void Establish()
    {
        _keys = Substitute.For<IMaterializationKeys>();
        _storage = Substitute.For<IMaterializationBindingStorage>();
        _keys.For(_store, _namespace, _key.Projection).Returns(_key);
        _catalog = new GenerationCatalog(_keys, _storage);
    }
}
