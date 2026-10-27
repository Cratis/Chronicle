// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_PublicApiBaseline;

/// <summary>
/// Invalid inventories fail loudly instead of silently producing partial protection.
/// </summary>
public class when_rendering_invalid_declarations : Specification
{
    Exception? _empty;
    Exception? _duplicate;
    Exception? _constructed;

    void Because()
    {
        _empty = Catch.Exception(() => given.public_api_snapshot.Render([]));
        _duplicate = Catch.Exception(() => given.public_api_snapshot.Render([typeof(given.a_sample_message), typeof(given.a_sample_message)]));
        _constructed = Catch.Exception(() => given.public_api_snapshot.Render([typeof(List<string>)]));
    }

    [Fact] void should_reject_an_empty_inventory() => _empty.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_reject_duplicate_declarations() => _duplicate.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_reject_constructed_type_declarations() => _constructed.ShouldBeOfExactType<NotSupportedException>();
}
