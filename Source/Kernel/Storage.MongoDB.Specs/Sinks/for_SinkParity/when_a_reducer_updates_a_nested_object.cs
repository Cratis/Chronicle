// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_SinkParity;

[Collection(MongoDBCollection.Name)]
public class when_a_reducer_updates_a_nested_object(when_a_reducer_updates_a_nested_object.context ctx) : IClassFixture<when_a_reducer_updates_a_nested_object.context>
{
    public class context(MongoDBFixture fixture) : given.a_parity_scenario(fixture)
    {
        protected override Type ReadModelType => typeof(Profile);

        protected override IReadOnlyList<Func<ExpandoObject>> States =>
        [
            () => CreateProfile("Oslo", "Norway"),
            () => CreateProfile("Bergen", "Norway")
        ];

        static ExpandoObject CreateProfile(string city, string country) =>
            Expando(
                ("id", "root-1"),
                ("home", Expando(("city", city), ("country", country))));

        record Address(string City, string Country);
        record Profile(string Id, Address Home);
    }

    [Fact] void should_apply_identically_across_sinks() => ctx.ParityReport.ShouldEqual(string.Empty);
}
