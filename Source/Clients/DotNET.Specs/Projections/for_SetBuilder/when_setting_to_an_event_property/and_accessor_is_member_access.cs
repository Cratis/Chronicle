// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_SetBuilder.when_setting_to_an_event_property;

public class and_accessor_is_member_access : Specification
{
    public record SomeEvent(string Name);
    public record ReadModel(string Name);

    Exception? _error;

    void Because()
    {
        var builder = new SetBuilder<ReadModel, SomeEvent, string, object>(new object(), new PropertyPath("Name"), new DefaultNamingPolicy());
        _error = Catch.Exception(() => builder.To(e => e.Name));
    }

    [Fact] void should_not_throw() => _error.ShouldBeNull();
}
