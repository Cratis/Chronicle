// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Supplies store-local copies of discovered fluent wire definitions without changing their builders.
/// </summary>
internal interface IFluentProjectionDefinitionsSource
{
    /// <summary>
    /// Discovers definitions or returns independent copies of equivalent discovery.
    /// </summary>
    /// <param name="delivery">The generation delivery policy.</param>
    /// <param name="discover">The normal discovery path.</param>
    /// <returns>The discovered definitions, failures and read-model names.</returns>
    (Dictionary<Type, ProjectionDefinition> Definitions, Dictionary<Type, Exception> Failures, Dictionary<Type, string> ContainerNames) Get(
        Observation.EventGenerationDelivery delivery,
        Func<(Dictionary<Type, ProjectionDefinition> Definitions, Dictionary<Type, Exception> Failures, Dictionary<Type, string> ContainerNames)> discover);
}
