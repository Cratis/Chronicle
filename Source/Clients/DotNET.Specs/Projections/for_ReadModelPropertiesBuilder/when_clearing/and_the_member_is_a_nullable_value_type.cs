// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ReadModelPropertiesBuilder.when_clearing;

public class and_the_member_is_a_nullable_value_type : Specification
{
    public record ReadModel(int? Attempts, int Count);
    public record SomeEvent(string Name);

    Exception? _error;

    void Because()
    {
        var projectionBuilder = Substitute.For<IProjectionBuilder<ReadModel, IProjectionBuilderFor<ReadModel>>>();
        var builder = new FromBuilder<ReadModel, SomeEvent, IProjectionBuilderFor<ReadModel>>(projectionBuilder, new DefaultNamingPolicy());
        _error = Catch.Exception(() => builder.Clear(x => x.Attempts));
    }

    [Fact] void should_not_throw() => _error.ShouldBeNull();
}
