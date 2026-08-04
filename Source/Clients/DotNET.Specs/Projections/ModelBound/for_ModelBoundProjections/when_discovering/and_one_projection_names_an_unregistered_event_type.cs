// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjections.when_discovering;

/// <summary>
/// One read model that cannot be built used to cost every read model in the application. Discovery built the whole
/// set in a single expression, so the first failure abandoned the dictionary - and because the fluent definitions
/// are assigned after this call, they went with it. What a consumer was left with was a host that starts, answers
/// requests and appends events, with a read side that was never registered: no failed partition, no query-time
/// error, just collections that stay empty. That is indistinguishable from "nothing has happened yet".
/// </summary>
public class and_one_projection_names_an_unregistered_event_type : given.a_model_bound_projections
{
    IDictionary<Type, ProjectionDefinition> _result;

    void Establish() => _clientArtifactsProvider.ModelBoundProjections.Returns(
    [
        typeof(ProjectionNamingAnUnregisteredEvent),
        typeof(ParentProjection),
        typeof(ChildProjection)
    ]);

    void Because() => _result = projections.Discover();

    [Fact] void should_not_include_the_projection_that_could_not_be_built() => _result.Keys.ShouldNotContain(typeof(ProjectionNamingAnUnregisteredEvent));
    [Fact] void should_still_include_every_projection_that_could_be_built() => _result.Keys.ShouldContainOnly([typeof(ParentProjection), typeof(ChildProjection)]);
    [Fact] void should_report_the_read_model_that_was_lost() => _logger.ReceivedWithAnyArgs(1).Log(LogLevel.Warning, default, default(object)!, default, default!);
    [Fact] void should_report_it_as_a_type_that_is_not_an_event_type() => LoggedException.ShouldBeOfExactType<TypeIsNotAnEventType>();
    [Fact] void should_capture_only_the_read_model_that_was_lost_as_a_failure() => projections.Failures.Keys.ShouldContainOnly([typeof(ProjectionNamingAnUnregisteredEvent)]);
    [Fact] void should_capture_the_failure_that_stopped_it() => projections.Failures[typeof(ProjectionNamingAnUnregisteredEvent)].ShouldBeOfExactType<TypeIsNotAnEventType>();

    Exception LoggedException => (Exception)_logger.ReceivedCalls()
        .First(call => call.GetMethodInfo().Name == nameof(ILogger.Log))
        .GetArguments()[3]!;
}
