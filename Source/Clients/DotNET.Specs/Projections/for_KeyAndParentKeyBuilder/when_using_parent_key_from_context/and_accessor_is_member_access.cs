// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_KeyAndParentKeyBuilder.when_using_parent_key_from_context;

public class and_accessor_is_member_access : Specification
{
    public record SomeEvent(string Reference);

    Exception? _error;

    void Because()
    {
        var builder = new KeyAndParentKeyBuilder<SomeEvent, object>(new DefaultNamingPolicy());
        _error = Catch.Exception(() => builder.UsingParentKeyFromContext(c => c.Occurred));
    }

    [Fact] void should_not_throw() => _error.ShouldBeNull();
}
