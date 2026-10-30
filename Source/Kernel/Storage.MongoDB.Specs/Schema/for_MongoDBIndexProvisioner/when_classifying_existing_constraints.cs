// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.MongoDB.Events.Constraints;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Schema.for_MongoDBIndexProvisioner;

[Collection(MongoDBCollection.Name)]
public class when_classifying_existing_constraints(MongoDBFixture fixture) : given.a_schema_database(fixture)
{
    const string NamedValues = "event-log+values+constraint";
    const string Unknown = "event-log+unknown+constraint";
    const string Ambiguous = "event-log+email+values+constraint";
    MongoDBIndexReport _before;
    MongoDBIndexReport _after;
    MongoDBIndexReport _verified;
    ILogger _logger;

    async Task Establish()
    {
        var constraints = new ConstraintsStorage(_database.GetEventStoreDatabase(Store));
        await constraints.SaveDefinition(new UniqueConstraintDefinition("values", []) { Mode = UniqueConstraintMode.PerValue });
        await constraints.SaveDefinition(new UniqueConstraintDefinition("values", []));
        await constraints.SaveDefinition(new UniqueConstraintDefinition("email", []) { Mode = UniqueConstraintMode.PerValue });
        await constraints.SaveDefinition(new UniqueConstraintDefinition("email+values", []));
        foreach (var collection in new[] { NamedValues, Unknown, Ambiguous }) await _namespace.CreateCollectionAsync(collection);
        _logger = Substitute.For<ILogger>();
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        var factory = Substitute.For<ILoggerFactory>();
        factory.CreateLogger(Arg.Any<string>()).Returns(_logger);
        _provisioner = new(_database, _clients, _mongoOptions, _storageOptions, factory);
    }

    async Task Because()
    {
        _indexTargets.Clear();
        _before = await _provisioner.Verify([Store]);
        _after = await _provisioner.Migrate([Store]);
        _verified = await _provisioner.Verify([Store]);
    }

    [Fact] void should_report_unknown_and_conflicting_collections_during_verify() => _before.Scopes.Single(scope => scope.Scope == $"namespace:{Store}/{Namespace}").Unclassified.Count.ShouldEqual(2);
    [Fact] async Task should_use_the_latest_stored_mode_for_a_per_source_constraint_named_values() => (await Indexes(_namespace, NamedValues)).Any(index => index["name"] == "value").ShouldBeTrue();
    [Fact] async Task should_not_infer_an_owner_index_from_the_values_suffix() => (await Indexes(_namespace, NamedValues)).Any(index => index["name"] == "owner").ShouldBeFalse();
    [Fact] void should_not_issue_index_ddl_for_unclassified_collections() => _indexTargets.Any(target => IsAnyOf(target.Collection, Unknown, Ambiguous)).ShouldBeFalse();
    [Fact] async Task should_leave_an_unknown_collection_with_only_its_identity_index() => (await Indexes(_namespace, Unknown)).Count.ShouldEqual(1);
    [Fact] async Task should_leave_an_ambiguous_collection_with_only_its_identity_index() => (await Indexes(_namespace, Ambiguous)).Count.ShouldEqual(1);
    [Fact] void should_keep_unclassified_collections_advisory_after_migration() => _after.IsCurrent.ShouldBeTrue();
    [Fact] void should_keep_unclassified_collections_advisory_during_verify() => _verified.IsCurrent.ShouldBeTrue();
    [Fact] void should_keep_unclassified_names_in_the_report() => _verified.Scopes.Single(scope => scope.Scope == $"namespace:{Store}/{Namespace}").Unclassified.Contains(Ambiguous).ShouldBeTrue();
    [Fact] void should_warn_about_unclassified_collections() => _logger.ReceivedCalls().Any(call => call.GetMethodInfo().Name == "Log" && (LogLevel)call.GetArguments()[0]! == LogLevel.Warning && call.GetArguments()[2]!.ToString()!.Contains("unclassified", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_explain_runtime_index_creation_in_the_migration_warning() => _logger.ReceivedCalls().Any(call => call.GetMethodInfo().Name == "Log" && call.GetArguments()[2]!.ToString()!.Contains("Runtime will create their indexes on first access", StringComparison.Ordinal)).ShouldBeTrue();
}
