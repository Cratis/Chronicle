// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Models;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Definitions;

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
            Identifier = definition.Identifier,
            Name = definition.Name,
            Model = definition.Model.ToContract(),
            IsActive = definition.IsActive,
            IsRewindable = definition.IsRewindable,
            InitialModelState = definition.InitialModelState.ToJsonString(),
            From = definition.From.ToDictionary(_ => _.Key.ToContract(), _ => _.Value.ToContract()),
            Join = definition.Join.ToDictionary(_ => _.Key.ToContract(), _ => _.Value.ToContract()),
            Children = definition.Children.ToDictionary(_ => (string)_.Key, _ => _.Value.ToContract()),
            FromAny = definition.FromAny.Select(_ => _.ToContract()).ToList(),
            Sink = definition.Sink.ToContract(),
            All = definition.All.ToContract(),
            FromEventProperty = definition.FromEventProperty?.ToContract() ?? null!,
            RemovedWith = definition.RemovedWith?.ToContract() ?? null!,
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
            contract.Identifier,
            contract.Name,
            contract.Model.ToChronicle(),
            contract.IsActive,
            contract.IsRewindable,
            (JsonObject)JsonNode.Parse(contract.InitialModelState)! ?? [],
            contract.From.ToDictionary(_ => _.Key.ToChronicle(), _ => _.Value.ToChronicle()),
            contract.Join.ToDictionary(_ => _.Key.ToChronicle(), _ => _.Value.ToChronicle()),
            contract.Children.ToDictionary(_ => new PropertyPath(_.Key), _ => _.Value.ToChronicle()),
            contract.FromAny.Select(_ => _.ToChronicle()),
            contract.All.ToChronicle(),
            contract.Sink.ToChronicle(),
            contract.FromEventProperty?.ToChronicle() ?? null!,
            contract.RemovedWith?.ToChronicle() ?? null!,
            contract.LastUpdated ?? null!
        );
    }
}
