// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Decisions;

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Exposes the additive decision-read gRPC service without extending the existing IServices contract.
/// </summary>
public interface IDecisionReadModelsServiceAccessor
{
    /// <summary>
    /// Gets the generated decision-read service.
    /// </summary>
    IDecisionReadModels DecisionReadModels { get; }
}
