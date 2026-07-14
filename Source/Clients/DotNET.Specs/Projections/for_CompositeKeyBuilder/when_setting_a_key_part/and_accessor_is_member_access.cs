// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_CompositeKeyBuilder.when_setting_a_key_part;

public class and_accessor_is_member_access : Specification
{
    public record KeyType(string Name);
    public record SomeEvent(string Name);

    Exception? _error;

    void Because()
    {
        var builder = new CompositeKeyBuilder<KeyType, SomeEvent>(new DefaultNamingPolicy());
        _error = Catch.Exception(() => builder.Set(x => x.Name));
    }

    [Fact] void should_not_throw() => _error.ShouldBeNull();
}
