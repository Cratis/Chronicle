// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Storage.Sql.EventStores.Generations;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Generations.for_GenerationInitializationStorage.when_blocker_subjects_differ_only_in_spelling;

[Collection(SqlServerCollection.Name)]
public class and_the_spellings_differ_in_trailing_space(SqlServerFixture fixture) : given.a_sqlserver_initialization_storage(fixture)
{
    static readonly DateTimeOffset _at = DateTimeOffset.FromUnixTimeSeconds(30000);
    Exception? _error;
    IReadOnlyList<GenerationInitializationBlocker> _blockers = [];

    GenerationInitializationBlocker Blocker(string subject, string detail) => new(new("orders"), GenerationInitializationBlockerKind.ExclusionNotProven, subject, detail, _at, null);

    async Task Because()
    {
        await _audit.RecordBlocker(Blocker("alice", "first"));
        try
        {
            await _audit.RecordBlocker(Blocker("alice ", "second"));
        }
        catch (Exception error)
        {
            _error = error;
        }

        await _audit.RecordBlocker(Blocker("alice", "updated"));
        _blockers = await _audit.GetBlockers();
    }

    [Fact] void should_refuse_the_differently_spelled_subject() => _error.ShouldBeOfExactType<GenerationBlockerSubjectSpellingConflict>();
    [Fact] void should_keep_a_single_row() => _blockers.Count.ShouldEqual(1);
    [Fact] void should_update_the_exactly_spelled_row() => _blockers[0].Detail.ShouldEqual("updated");
    [Fact] void should_keep_the_stored_spelling() => _blockers[0].Subject.ShouldEqual("alice");
}
