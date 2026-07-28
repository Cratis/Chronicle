// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Storage.Jobs;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Jobs.for_JobStorage;

/// <summary>
/// The real server persists <see cref="JobState"/> through the custom, non-document <see cref="JobStateSerializer"/>,
/// which the MongoDB LINQ provider cannot introspect. Every job query filter must therefore render against that
/// serializer - a property-expression filter over an enum member throws ExpressionNotSupportedException instead.
/// </summary>
public class when_rendering_the_job_filters_against_the_server_serializer : Specification
{
    static readonly JobStatus[] _statuses = [JobStatus.Running, JobStatus.Failed];
    static readonly JobType _jobType = new("some-job-type");

    IBsonSerializer<JobState> _serializer;
    BsonDocument _renderedStatusFilter;
    BsonDocument _renderedTypeAndStatusFilter;
    BsonDocument _renderedTypeOnlyFilter;
    Exception? _propertyExpressionError;

    void Establish() => _serializer = new JobStateSerializer(Substitute.For<IJobTypes>());

    void Because()
    {
        _renderedStatusFilter = Render(JobStorage.StatusFilter<JobState>(_statuses));
        _renderedTypeAndStatusFilter = Render(JobStorage.TypeAndStatusFilter<JobState>(_jobType, _statuses));
        _renderedTypeOnlyFilter = Render(JobStorage.TypeAndStatusFilter<JobState>(_jobType, []));

        // The exact form the LINQ provider chokes on against the custom serializer - kept here so the regression is
        // explicit about what must never be reintroduced.
        _propertyExpressionError = TryRender(Builders<JobState>.Filter.Eq(_ => _.Status, JobStatus.Running));
    }

    BsonDocument Render(FilterDefinition<JobState> filter) =>
        filter.Render(new RenderArgs<JobState>(_serializer, BsonSerializer.SerializerRegistry));

    Exception? TryRender(FilterDefinition<JobState> filter)
    {
        try
        {
            Render(filter);
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    [Fact] void should_render_the_status_filter_on_the_stored_status_element() => _renderedStatusFilter.ToString().ShouldContain("status");
    [Fact] void should_render_the_status_filter_as_an_alternation() => _renderedStatusFilter.Contains("$or").ShouldBeTrue();
    [Fact] void should_render_the_type_and_status_filter_on_the_stored_type_element() => _renderedTypeAndStatusFilter.ToString().ShouldContain("type");
    [Fact] void should_render_the_type_only_filter_without_narrowing_on_status() => _renderedTypeOnlyFilter.ToString().ShouldNotContain("status");
    [Fact] void should_confirm_the_property_expression_form_cannot_be_rendered() => _propertyExpressionError.ShouldNotBeNull();
}
