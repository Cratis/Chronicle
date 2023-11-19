// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Suggestions;

namespace Aksio.Cratis.Kernel.Grains.Suggestions;

/// <summary>
/// Exception that gets thrown when a suggestion does not exist.
/// </summary>
public class UnknownSuggestion : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnknownSuggestion"/> class.
    /// </summary>
    /// <param name="microserviceId">The <see cref="MicroserviceId"/> the suggestion wasn't found in.</param>
    /// <param name="tenantId">The <see cref="TenantId"/> the suggestion wasn't found for.</param>
    /// <param name="suggestionId">The <see cref="SuggestionId"/> that wasn't found.</param>
    public UnknownSuggestion(
        MicroserviceId microserviceId,
        TenantId tenantId,
        SuggestionId suggestionId) : base($"Unknown suggestion with id {suggestionId} for tenant {tenantId} in microservice {microserviceId}")
    {
    }
}
