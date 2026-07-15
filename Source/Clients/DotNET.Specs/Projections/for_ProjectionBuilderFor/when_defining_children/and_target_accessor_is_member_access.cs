// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ProjectionBuilderFor.when_defining_children;

public class and_target_accessor_is_member_access : Specification
{
    public record Child(string Value);
    public record ReadModel(IEnumerable<Child> Items);

    Exception? _error;

    void Because()
    {
        var eventTypes = new EventTypesForSpecifications([]);
        var builder = new ProjectionBuilderFor<ReadModel>(
            new ProjectionId(typeof(ReadModel).FullName),
            typeof(ReadModel),
            new DefaultNamingPolicy(),
            eventTypes,
            new JsonSerializerOptions());
        _error = Catch.Exception(() => builder.Children(_ => _.Items, _ => { }));
    }

    [Fact] void should_not_throw() => _error.ShouldBeNull();
}
