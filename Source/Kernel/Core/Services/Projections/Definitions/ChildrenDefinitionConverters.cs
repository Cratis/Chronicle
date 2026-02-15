// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            FromEventProperty = definition.FromEventProperty?.ToContract() ?? null!,
            RemovedWith = definition.RemovedWith.ToDictionary(_ => _.Key.ToContract(), _ => _.Value.ToContract()),
            RemovedWithJoin = definition.RemovedWithJoin.ToDictionary(_ => _.Key.ToContract(), _ => _.Value.ToContract())
        };
    }

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
            contract.FromEventProperty?.ToChronicle() ?? null!
        );
    }
}
