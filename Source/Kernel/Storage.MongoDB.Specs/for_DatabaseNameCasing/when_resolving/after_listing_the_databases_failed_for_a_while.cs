// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_DatabaseNameCasing.when_resolving;

/// <summary>
/// A failure to list the databases other than a refused permission is passed on without claiming the name, so once
/// the server answers again the existing casing is found instead of a case variant that MongoDB refuses to create.
/// </summary>
public class after_listing_the_databases_failed_for_a_while : Specification
{
    IMongoClient _client;
    Exception _failure;
    string _resolved;

    void Establish()
    {
        var calls = 0;
        _client = Substitute.For<IMongoClient>();
        _client.ListDatabaseNames(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            if (calls++ == 0)
            {
                throw new TimeoutException("A timeout occurred after 30000ms selecting a server");
            }

            var cursor = Substitute.For<IAsyncCursor<string>>();
            cursor.MoveNext(Arg.Any<CancellationToken>()).Returns(true, false);
            cursor.Current.Returns(["Studio+es+Cratis"]);
            return cursor;
        });
    }

    void Because()
    {
        _failure = Catch.Exception(() => DatabaseNameCasing.Resolve(_client, "Studio+es+cratis"));
        _resolved = DatabaseNameCasing.Resolve(_client, "Studio+es+cratis");
    }

    [Fact] void should_pass_on_the_failure() => _failure.ShouldBeOfExactType<TimeoutException>();
    [Fact] void should_resolve_to_the_existing_casing_once_the_server_answers() => _resolved.ShouldEqual("Studio+es+Cratis");
}
