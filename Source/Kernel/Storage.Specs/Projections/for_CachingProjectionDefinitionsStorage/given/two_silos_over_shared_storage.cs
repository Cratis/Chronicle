// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Storage.Projections.for_CachingProjectionDefinitionsStorage.given;

public class two_silos_over_shared_storage : Specification
{
    protected SharedProjectionDefinitionsStorage _shared;
    protected CachingProjectionDefinitionsStorage _firstSilo;
    protected CachingProjectionDefinitionsStorage _secondSilo;
    protected ProjectionId _id;
    protected ProjectionDefinition _definition;

    void Establish()
    {
        _shared = new();
        _firstSilo = new(_shared);
        _secondSilo = new(_shared);
        _id = "some-projection";
        _definition = a_caching_projection_definitions_storage.DefinitionFor(_id);
    }
}
