// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ReadModelPropertiesBuilder.when_counting;

public class and_accessor_is_member_access : Specification
{
    public record ReadModel(int Count, string Name);
    public record SomeEvent(int Amount, string Name);

    Exception? _error;

    void Because()
    {
        var projectionBuilder = Substitute.For<IProjectionBuilder<ReadModel, IProjectionBuilderFor<ReadModel>>>();
        var builder = new FromBuilder<ReadModel, SomeEvent, IProjectionBuilderFor<ReadModel>>(projectionBuilder, new DefaultNamingPolicy());
        _error = Catch.Exception(() => builder.Count(x => x.Count));
    }

    [Fact] void should_not_throw() => _error.ShouldBeNull();
}
