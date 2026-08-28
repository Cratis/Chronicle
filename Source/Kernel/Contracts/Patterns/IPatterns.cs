// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Patterns;

/// <summary>
/// Defines the contract for querying the behavior patterns mined from event history.
/// </summary>
[Service]
public interface IPatterns
{
    /// <summary>
    /// Get the patterns that apply to a partial context, ranked by specificity and confidence.
    /// </summary>
    /// <param name="request">The <see cref="GetPatternsRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns><see cref="IEnumerable{T}"/> of <see cref="Pattern"/>, empty when nothing clears the confidence bar.</returns>
    [Operation]
    Task<IEnumerable<Pattern>> GetPatterns(GetPatternsRequest request, CallContext context = default);

    /// <summary>
    /// Get every pattern held for a scope.
    /// </summary>
    /// <param name="request">The <see cref="GetPatternsForScopeRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns><see cref="IEnumerable{T}"/> of <see cref="Pattern"/>.</returns>
    /// <remarks>
    /// This is the browsing call - everything a scope has established, unfiltered - as opposed to
    /// <see cref="GetPatterns"/>, which answers a question about one situation.
    /// </remarks>
    [Operation]
    Task<IEnumerable<Pattern>> GetPatternsForScope(GetPatternsForScopeRequest request, CallContext context = default);
}
