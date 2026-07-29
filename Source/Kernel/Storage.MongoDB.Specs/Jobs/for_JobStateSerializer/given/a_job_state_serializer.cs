// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Concepts.Jobs;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Jobs.for_JobStateSerializer.given;

public class a_job_state_serializer : Specification
{
    protected IJobTypes _jobTypes;
    protected JobStateSerializer _serializer;

    static a_job_state_serializer()
    {
        RegisterConceptSerializers();

        // JobState itself is mapped by the server's own JobStateClassMap, registered for the whole assembly in
        // SpecSerializationSetup. Mapping it here instead would unmap nothing and leave Request mapped, giving
        // these specs a document shape the silo never writes.
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
