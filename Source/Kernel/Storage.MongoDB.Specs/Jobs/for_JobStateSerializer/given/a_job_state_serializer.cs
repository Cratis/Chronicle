// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Storage.Jobs;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Jobs.for_JobStateSerializer.given;

public class a_job_state_serializer : Specification
{
    protected IJobTypes _jobTypes;
    protected JobStateSerializer _serializer;

    static a_job_state_serializer()
    {
        RegisterConceptSerializers();

        if (!BsonClassMap.IsClassMapRegistered(typeof(JobState)))
        {
            BsonClassMap.RegisterClassMap<JobState>(cm =>
            {
                cm.AutoMap();
                cm.MapMember(c => c.Id);
                cm.MapMember(c => c.Details);
                cm.MapMember(c => c.Type);
                cm.MapMember(c => c.Status);
                cm.MapMember(c => c.Created);
                cm.MapMember(c => c.StatusChanges);
                cm.MapMember(c => c.Progress);
                cm.MapMember(c => c.Request);
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(SampleJobRequest)))
        {
            BsonClassMap.RegisterClassMap<SampleJobRequest>(cm =>
            {
                cm.AutoMap();
                cm.MapMember(c => c.Name);
                cm.MapMember(c => c.Count);
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(AnotherJobRequest)))
        {
            BsonClassMap.RegisterClassMap<AnotherJobRequest>(cm =>
            {
                cm.AutoMap();
                cm.MapMember(c => c.Id);
                cm.MapMember(c => c.Description);
            });
        }
    }

    static void RegisterConceptSerializers()
    {
        BsonSerializer.RegisterSerializationProvider(new ConceptSerializationProvider());
    }

    void Establish()
    {
        _jobTypes = Substitute.For<IJobTypes>();
        _serializer = new JobStateSerializer(_jobTypes);
    }
}
