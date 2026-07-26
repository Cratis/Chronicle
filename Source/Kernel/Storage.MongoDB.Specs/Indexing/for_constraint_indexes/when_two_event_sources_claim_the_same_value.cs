// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Chronicle.Storage.MongoDB.Events.Constraints;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.MongoDB.Indexing.for_constraint_indexes;

/// <summary>
/// A claim that races past validation is settled by the unique index, which reports the loser as a duplicate-key
/// write error. Callers must see that as a constraint violation, not as a MongoDB driver exception they would have
/// to recognize by error category.
/// </summary>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/> providing a MongoDB container.</param>
[Collection(MongoDBCollection.Name)]
public class when_two_event_sources_claim_the_same_value(MongoDBFixture fixture) : given.a_real_namespace_database(fixture)
{
    const string ConstraintName = "claimed-constraint";
    static readonly UniqueConstraintValue _value = "the-claimed-value";

    Exception? _error;

    async Task Because()
    {
        var storage = new UniqueConstraintsStorage(_database, EventSequenceId.Log, Substitute.For<ILogger<UniqueConstraintsStorage>>());
        await storage.Save((EventSourceId)"es-1", ConstraintName, 0, _value);

        try
        {
            await storage.Save((EventSourceId)"es-2", ConstraintName, 1, _value);
        }
        catch (Exception ex)
        {
            _error = ex;
        }
    }

    [Fact] void should_report_it_as_a_duplicate_unique_constraint_value() => _error.ShouldBeOfExactType<DuplicateUniqueConstraintValue>();
    [Fact] void should_name_the_constraint_that_was_violated() => ((DuplicateUniqueConstraintValue)_error!).ConstraintName.ShouldEqual((ConstraintName)ConstraintName);
    [Fact] void should_name_the_event_source_that_lost_the_claim() => ((DuplicateUniqueConstraintValue)_error!).EventSourceId.ShouldEqual((EventSourceId)"es-2");
}
