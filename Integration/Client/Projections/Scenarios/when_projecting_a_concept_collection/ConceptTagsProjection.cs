// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Projections.Events;
using Cratis.Chronicle.Integration.Projections.Scenarios.ReadModels;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_a_concept_collection;

/// <summary>
/// Projects <see cref="ConceptTagsAssigned"/> into a <see cref="ConceptTagsReadModel"/>, auto-mapping the
/// whole concept collection onto the read model so its persistence through the sink can be verified.
/// </summary>
public class ConceptTagsProjection : IProjectionFor<ConceptTagsReadModel>
{
    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<ConceptTagsReadModel> builder) => builder
        .AutoMap()
        .From<ConceptTagsAssigned>();
}
