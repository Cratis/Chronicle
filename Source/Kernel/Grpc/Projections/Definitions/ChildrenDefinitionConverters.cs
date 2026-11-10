// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Services.Projections.Definitions;

/// <summary>
/// Converter methods for <see cref="ChildrenDefinition"/>.
/// </summary>
internal static class ChildrenDefinitionConverters
{
    /// <summary>
    /// Convert to contract version of <see cref="ChildrenDefinition"/>.
    /// </summary>
    /// <param name="definition"><see cref="ChildrenDefinition"/> to convert.</param>
    /// <returns>Converted contract version.</returns>
    public static Contracts.Projections.ChildrenDefinition ToContract(this ChildrenDefinition definition)
    {
        return new()
        {
            IdentifiedBy = definition.IdentifiedBy,
            From = definition.From.ToDictionary(_ => _.Key.ToContract(), _ => _.Value.ToContract()),
            Join = definition.Join.ToDictionary(_ => _.Key.ToContract(), _ => _.Value.ToContract()),
            Children = definition.Children.ToDictionary(_ => (string)_.Key, _ => _.Value.ToContract()),
            All = definition.FromEvery.ToContract(),
            FromEventProperty = definition.FromEventProperty?.ToContract(),
            RemovedWith = definition.RemovedWith.ToDictionary(_ => _.Key.ToContract(), _ => _.Value.ToContract()),
            RemovedWithJoin = definition.RemovedWithJoin.ToDictionary(_ => _.Key.ToContract(), _ => _.Value.ToContract()),
            AutoMap = (Contracts.Projections.AutoMap)definition.AutoMap,
            Nested = (definition.Nested ?? new Dictionary<PropertyPath, ChildrenDefinition>()).ToDictionary(_ => (string)_.Key, _ => _.Value.ToContract()),
            NoAutoMapProperties = [.. (definition.NoAutoMapProperties ?? []).Select(_ => (string)_)],
            Ordering = definition.Ordering?.ToContract()
        };
    }

    /// <summary>
    /// Convert to contract version of <see cref="ChildrenOrdering"/>.
    /// </summary>
    /// <param name="ordering"><see cref="ChildrenOrdering"/> to convert.</param>
    /// <returns>Converted contract version, or null for the default append ordering.</returns>
    public static Contracts.Projections.ChildrenOrderingDefinition? ToContract(this ChildrenOrdering ordering) =>
        ordering.Placement == ChildrenPlacement.Append
            ? null
            : new()
            {
                Placement = (Contracts.Projections.ChildrenPlacement)ordering.Placement,
                SortBy = ordering.SortBy.IsSet ? ordering.SortBy.Path : string.Empty,
                Direction = (Contracts.Projections.SortDirection)ordering.Direction
            };

    /// <summary>
    /// Convert to Chronicle version of <see cref="Contracts.Projections.ChildrenOrderingDefinition"/>.
    /// </summary>
    /// <param name="contract"><see cref="Contracts.Projections.ChildrenOrderingDefinition"/> to convert.</param>
    /// <returns>Converted Chronicle version, or null for the default append ordering.</returns>
    public static ChildrenOrdering? ToChronicle(this Contracts.Projections.ChildrenOrderingDefinition contract) =>
        contract.Placement == Contracts.Projections.ChildrenPlacement.Append
            ? null
            : new(
                (ChildrenPlacement)contract.Placement,
                string.IsNullOrEmpty(contract.SortBy) ? PropertyPath.NotSet : new PropertyPath(contract.SortBy),
                (SortDirection)contract.Direction);

    /// <summary>
    /// Convert to Chronicle version of <see cref="ChildrenDefinition"/>.
    /// </summary>
    /// <param name="contract"><see cref="Contracts.Projections.ChildrenDefinition"/> to convert.</param>
    /// <returns>Converted Chronicle version.</returns>
    public static ChildrenDefinition ToChronicle(this Contracts.Projections.ChildrenDefinition contract)
    {
        return new(
            contract.IdentifiedBy,
            contract.From.ToDictionary(_ => _.Key.ToChronicle(), _ => _.Value.ToChronicle()),
            contract.Join.ToDictionary(_ => _.Key.ToChronicle(), _ => _.Value.ToChronicle()),
            contract.Children.ToDictionary(_ => new PropertyPath(_.Key), _ => _.Value.ToChronicle()),
            contract.All.ToChronicle(),
            contract.RemovedWith.ToDictionary(_ => _.Key.ToChronicle(), _ => _.Value.ToChronicle()),
            contract.RemovedWithJoin.ToDictionary(_ => _.Key.ToChronicle(), _ => _.Value.ToChronicle()),
            contract.FromEventProperty?.ToChronicle(),
            AutoMap: (AutoMap)contract.AutoMap,
            Nested: contract.Nested.ToDictionary(_ => new PropertyPath(_.Key), _ => _.Value.ToChronicle()),
            NoAutoMapProperties: [.. contract.NoAutoMapProperties.Select(_ => new PropertyPath(_))],
            Ordering: contract.Ordering?.ToChronicle()
        );
    }
}
