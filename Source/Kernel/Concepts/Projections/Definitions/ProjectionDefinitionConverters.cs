// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Models;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Concepts.Projections.Definitions;

/// <summary>
/// Converter methods for <see cref="ProjectionDefinition"/>.
/// </summary>
public static class ProjectionDefinitionConverters
{
    /// <summary>
    /// Convert to contract version of <see cref="ProjectionDefinition"/>.
    /// </summary>
    /// <param name="definition"><see cref="ProjectionDefinition"/> to convert.</param>
    /// <returns>Converted contract version.</returns>
    public static Contracts.Projections.ProjectionDefinition ToContract(this ProjectionDefinition definition)
    {
        return new()
        {
            EventSequenceId = definition.EventSequenceId,
            Identifier = definition.Identifier,
            Model = definition.Model.ToContract(),
            IsActive = definition.IsActive,
            IsRewindable = definition.IsRewindable,
            InitialModelState = definition.InitialModelState.ToJsonString(),
            From = definition.From.ToDictionary(_ => _.Key.ToContract(), _ => _.Value.ToContract()),
            Join = definition.Join.ToDictionary(_ => _.Key.ToContract(), _ => _.Value.ToContract()),
            Children = definition.Children.ToDictionary(_ => (string)_.Key, _ => _.Value.ToContract()),
            FromEvery = definition.FromDerivatives.Select(_ => _.ToContract()).ToList(),
            Sink = definition.Sink.ToContract(),
            All = definition.FromEvery.ToContract(),
            FromEventProperty = definition.FromEventProperty?.ToContract() ?? null!,
            RemovedWith = definition.RemovedWith.ToDictionary(_ => _.Key.ToContract(), _ => _.Value.ToContract()),
            RemovedWithJoin = definition.RemovedWithJoin.ToDictionary(_ => _.Key.ToContract(), _ => _.Value.ToContract()),
            LastUpdated = definition.LastUpdated ?? null!
        };
    }

    /// <summary>
    /// Convert to Chronicle version of <see cref="ProjectionDefinition"/>.
    /// </summary>
    /// <param name="contract"><see cref="Contracts.Projections.ProjectionDefinition"/> to convert.</param>
    /// <returns>Converted Chronicle version.</returns>
    public static ProjectionDefinition ToChronicle(this Contracts.Projections.ProjectionDefinition contract)
    {
        return new(
            contract.EventSequenceId,
            contract.Identifier,
            contract.Model.ToChronicle(),
            contract.IsActive,
            contract.IsRewindable,
            (JsonObject)JsonNode.Parse(contract.InitialModelState)! ?? [],
            contract.From.ToDictionary(_ => _.Key.ToChronicle(), _ => _.Value.ToChronicle()),
            contract.Join.ToDictionary(_ => _.Key.ToChronicle(), _ => _.Value.ToChronicle()),
            contract.Children.ToDictionary(_ => new PropertyPath(_.Key), _ => _.Value.ToChronicle()),
            contract.FromEvery.Select(_ => _.ToChronicle()),
            contract.All.ToChronicle(),
            contract.Sink.ToChronicle(),
            contract.RemovedWith.ToDictionary(_ => _.Key.ToChronicle(), _ => _.Value.ToChronicle()),
            contract.RemovedWithJoin.ToDictionary(_ => _.Key.ToChronicle(), _ => _.Value.ToChronicle()),
            contract.FromEventProperty?.ToChronicle() ?? null!,
            contract.LastUpdated ?? null!
        );
    }
}
