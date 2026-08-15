// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ReadModelPropertiesBuilder.when_clearing;

/// <summary>
/// The control's twin: a value type cannot hold null whatever the nullable context says, so the same call that is
/// accepted on int? is refused on int. Zero is a count rather than the absence of one.
/// </summary>
public class and_the_member_is_a_value_type : Specification
{
    public record ReadModel(int? Attempts, int Count);
    public record SomeEvent(string Name);

    Exception? _error;

    void Because()
    {
        var projectionBuilder = Substitute.For<IProjectionBuilder<ReadModel, IProjectionBuilderFor<ReadModel>>>();
        var builder = new FromBuilder<ReadModel, SomeEvent, IProjectionBuilderFor<ReadModel>>(projectionBuilder, new DefaultNamingPolicy());
        _error = Catch.Exception(() => builder.Clear(x => x.Count));
    }

    [Fact] void should_refuse_the_clear() => _error.ShouldBeOfExactType<CannotClearNonNullableMember>();
}
