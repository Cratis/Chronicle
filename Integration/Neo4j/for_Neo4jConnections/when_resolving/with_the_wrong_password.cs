// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Neo4j.Driver;
using context = Cratis.Chronicle.Neo4j.Integration.for_Neo4jConnections.when_resolving.with_the_wrong_password.context;

namespace Cratis.Chronicle.Neo4j.Integration.for_Neo4jConnections.when_resolving;

/// <summary>
/// A password that is not the one of the server is refused by the server when the connection is used.
/// </summary>
/// <param name="context">The context the facts assert against.</param>
[Collection(Neo4jCollection.Name)]
public class with_the_wrong_password(context context) : Neo4jGiven<context>(context)
{
    public class context(Neo4jFixture fixture) : Neo4jSpecification(fixture)
    {
        public Exception? Error;

        async Task Because()
        {
            var connection = await CreateConnections(CreateOptions(), "not-the-password").Resolve(EventStore, Namespace, ConnectionPurpose.Runtime);
            Error = await Catch.Exception(async () =>
            {
                await using var session = connection.OpenSystemSession();
                await (await session.RunAsync("RETURN 1")).ConsumeAsync();
            });
        }
    }

    [Fact] void should_not_be_let_in() => Context.Error.ShouldNotBeNull();
    [Fact] void should_fail_to_authenticate() => (Context.Error is AuthenticationException).ShouldBeTrue();
}
