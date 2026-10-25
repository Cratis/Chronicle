// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;

namespace Cratis.Chronicle.Projections.Generations;

/// <summary>
/// Serializes candidate admission under the key <c>{store}/{key.Value}</c> without dispatching work.
/// </summary>
public interface IProjectionGenerationBuild : IGrainWithStringKey
{
    /// <summary>
    /// Reserves a candidate and admits its fixed writer grant, or recovers the same operation's admission.
    /// </summary>
    /// <param name="request">The immutable request, including the caller's persisted operation identity and time.</param>
    /// <returns>The admission or a fail-closed refusal.</returns>
    Task<GenerationBuildAdmission> Start(GenerationBuildRequest request);
}
