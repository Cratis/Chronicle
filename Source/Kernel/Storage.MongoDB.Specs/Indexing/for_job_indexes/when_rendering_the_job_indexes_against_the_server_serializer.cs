// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Storage.Jobs;
using Cratis.Chronicle.Storage.MongoDB.Jobs;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Indexing.for_job_indexes;

/// <summary>
/// Reproduces the server-startup crash: the real server persists <see cref="JobState"/> through the custom,
/// non-document <see cref="JobStateSerializer"/>, which the MongoDB LINQ provider cannot introspect. Rendering
/// the job index keys against that serializer must succeed — a property-expression key over an enum member
/// throws ExpressionNotSupportedException, which aborted the silo during jobs rehydration.
/// </summary>
public class when_rendering_the_job_indexes_against_the_server_serializer : Specification
{
    IBsonSerializer<JobState> _serializer;
    int _renderFailures;
    Exception? _propertyExpressionError;

    void Establish() => _serializer = new JobStateSerializer(Substitute.For<IJobTypes>());

    void Because()
    {
        _renderFailures = JobStorage.Indexes.Count(index => TryRender(index.Keys) is not null);

        // The exact form the LINQ provider chokes on against the custom serializer — kept here so the regression
        // is explicit about what must never be reintroduced.
        _propertyExpressionError = TryRender(Builders<JobState>.IndexKeys.Ascending(_ => _.Status));
    }

    Exception? TryRender(IndexKeysDefinition<JobState> keys)
    {
        try
        {
            keys.Render(new RenderArgs<JobState>(_serializer, BsonSerializer.SerializerRegistry));
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    [Fact] void should_render_every_job_index_using_element_names() => _renderFailures.ShouldEqual(0);
    [Fact] void should_confirm_the_property_expression_form_cannot_be_rendered() => _propertyExpressionError.ShouldNotBeNull();
}
