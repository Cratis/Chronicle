// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_FromEveryBuilder.when_setting_same_property_twice;

public class and_set_is_called_twice : Specification
{
    public record ReadModel(string Name);

    Exception? _error;

    void Because()
    {
        var builder = new FromEveryBuilder<ReadModel>(new DefaultNamingPolicy());
        builder.Set(x => x.Name).ToEventSourceId();
        _error = Catch.Exception(() => builder.Set(x => x.Name));
    }

    [Fact] void should_throw_duplicate_property_in_projection() => _error.ShouldBeOfExactType<DuplicatePropertyInProjection>();
    [Fact] void should_include_property_name_in_message() => _error?.Message.ShouldContain("Name");
}
