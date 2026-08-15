// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ReadModelPropertiesBuilder.when_clearing;

/// <summary>
/// The fluent builder is held to the same ruling as the attributes. C# cannot refuse this at the signature - a
/// non-nullable argument converts to a nullable parameter without complaint - so the definition build refuses it.
/// </summary>
public class and_the_member_is_not_nullable : Specification
{
    public record ReadModel(string? Note, string Name);
    public record SomeEvent(string Name);

    Exception? _error;

    void Because()
    {
        var projectionBuilder = Substitute.For<IProjectionBuilder<ReadModel, IProjectionBuilderFor<ReadModel>>>();
        var builder = new FromBuilder<ReadModel, SomeEvent, IProjectionBuilderFor<ReadModel>>(projectionBuilder, new DefaultNamingPolicy());
        _error = Catch.Exception(() => builder.Clear(x => x.Name));
    }

    [Fact] void should_refuse_the_clear() => _error.ShouldBeOfExactType<CannotClearNonNullableMember>();
    [Fact] void should_name_the_member_that_cannot_be_cleared() => _error!.Message.ShouldContain(nameof(ReadModel.Name));
}
