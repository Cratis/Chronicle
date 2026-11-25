// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Projections.Engine.Expressions.Keys;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.for_KeyResolvers;

public class when_resolving_flat_composite_keys : Specification
{
    KeyResolvers _resolvers;

    void Establish() => _resolvers = new KeyResolvers(NullLogger<KeyResolvers>.Instance);

    [Fact]
    async Task should_refuse_an_operator_shaped_expando()
    {
        var value = new ExpandoObject();
        ((IDictionary<string, object?>)value)["$ne"] = null;
        await AssertRefused(value);
    }

    [Fact] async Task should_refuse_an_object() => await AssertRefused(new object());
    [Fact] async Task should_refuse_a_dictionary() => await AssertRefused(new Dictionary<string, object> { ["$gt"] = 0 });
    [Fact] async Task should_refuse_an_array() => await AssertRefused(new[] { "one", "two" });
    [Fact] async Task should_refuse_a_collection() => await AssertRefused(new List<int> { 1, 2 });

    [Theory]
    [InlineData("customer")]
    [InlineData(42)]
    [InlineData(true)]
    async Task should_preserve_scalar_values(object value)
    {
        var resolved = (ResolvedKey)await Resolve(value);
        ((FlatCompositeKey)resolved.Key).Parts["customer"].ShouldEqual(value);
    }

    async Task AssertRefused(object value)
    {
        var error = await Catch.Exception(() => Resolve(value));
        error.ShouldBeOfExactType<FlatCompositeKeyNotSupported>();
        error.Message.ShouldContain("customer");
        error.Message.ShouldContain("scalar");
    }

    Task<KeyResolverResult> Resolve(object value) => _resolvers.FlatComposite(
        [("customer", _ => value), ("number", _ => 1)])(
        Substitute.For<IEventSequenceStorage>(), NullSink.Instance, AppendedEvent.Empty());
}
