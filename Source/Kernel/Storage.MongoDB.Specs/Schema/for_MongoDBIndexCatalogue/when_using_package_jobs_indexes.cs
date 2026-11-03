// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.MongoDB;
using Cratis.Orleans.Storage.MongoDB.Jobs;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Schema.for_MongoDBIndexCatalogue;

[Collection(MongoDBCollection.Name)]
public class when_using_package_jobs_indexes(MongoDBFixture fixture) : given.a_schema_database(fixture)
{
    IMongoDatabase _packageDatabase;
    IMongoDatabase _catalogueDatabase;
    bool _jobsRead;
    bool _stepsRead;
    bool _stepPlansRead;

    static string Signature(IEnumerable<BsonDocument> indexes) => string.Join('\n', indexes
        .OrderBy(index => index["name"].AsString, StringComparer.Ordinal)
        .Select(index => $"{index["name"]}:{index["key"].ToJson()}:{index.GetValue("unique", false)}:{index.GetValue("sparse", false)}:{index.GetValue("collation", BsonNull.Value)}"));

    async Task Because()
    {
        _packageDatabase = _client.GetDatabase($"{_prefix}package");
        _catalogueDatabase = _client.GetDatabase($"{_prefix}catalogue");
        _jobsRead = (await new JobStorage(_packageDatabase, Substitute.For<IJobTypes>()).GetJobs()).IsSuccess;
        _stepsRead = (await new JobStepStorage(_packageDatabase).GetForJob(JobId.New())).IsSuccess;
        _stepPlansRead = (await new JobStepPlanStorage(_packageDatabase, TransientRetry.Default).GetForJob(JobId.New(), Guid.NewGuid())).IsSuccess;
        foreach (var definition in MongoDBIndexCatalogue.ForJobs(_catalogueDatabase)) await definition.Ensure();
    }

    [Fact] void should_exercise_real_jobs_storage() => _jobsRead.ShouldBeTrue();
    [Fact] void should_exercise_real_job_steps_storage() => _stepsRead.ShouldBeTrue();
    [Fact] void should_exercise_real_job_step_plans_storage() => _stepPlansRead.ShouldBeTrue();
    [Fact] async Task should_match_jobs_names_keys_and_options() => await ShouldMatch(WellKnownCollectionNames.Jobs);
    [Fact] async Task should_match_active_step_names_keys_and_options() => await ShouldMatch(WellKnownCollectionNames.JobSteps);
    [Fact] async Task should_match_failed_step_names_keys_and_options() => await ShouldMatch(WellKnownCollectionNames.FailedJobSteps);
    [Fact] async Task should_match_step_plan_names_keys_and_options() => await ShouldMatch(WellKnownCollectionNames.JobStepPlans);
    [Fact] void should_pin_the_package_version() => typeof(JobStorage).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion.Split('+')[0].ShouldEqual("1.34.0");

    async Task ShouldMatch(string collection)
    {
        var package = await Indexes(_packageDatabase, collection);
        var catalogue = await Indexes(_catalogueDatabase, collection);
        Signature(catalogue).ShouldEqual(Signature(package));
    }
}
