// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ReadModelPropertiesBuilder.given;

public class a_from_builder_with_dictionaries : Specification
{
    public record Slot(string Name);
    public record ReadModel(Dictionary<string, string> Slots, Dictionary<string, int> Counts, IDictionary<string, decimal> Totals, string Name, string CreatedBy);
    public record SomeEvent(string Category, Slot Slot, string SeriesId, decimal Amount, string Name);

    protected FromBuilder<ReadModel, SomeEvent, IProjectionBuilderFor<ReadModel>> _builder;
    protected FromDefinition _result;

    protected virtual INamingPolicy NamingPolicy => new DefaultNamingPolicy();

    void Establish()
    {
        var projectionBuilder = Substitute.For<IProjectionBuilder<ReadModel, IProjectionBuilderFor<ReadModel>>>();
        _builder = new(projectionBuilder, NamingPolicy);
    }
}
