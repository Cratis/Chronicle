// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.Reducers;
using Cratis.Json;
using Cratis.Serialization;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Represents a lightweight, in-process scenario for testing read model projections and reducers without any infrastructure.
/// </summary>
/// <remarks>
/// <para>
/// Automatically detects how <typeparamref name="TReadModel"/> is projected — either via a reducer
/// (<see cref="IReducerFor{TReadModel}"/>), a fluent projection (<see cref="IProjectionFor{TReadModel}"/>),
/// or a model-bound projection — and routes events through the appropriate engine.
/// </para>
/// <para>
/// Usage:
/// <code>
/// var scenario = new ReadModelScenario&lt;MyReadModel&gt;();
/// scenario.Given([new SomeEvent(), new SomeOtherEvent()]);
/// scenario.Instance.SomeProperty.ShouldBe(expectedValue);
/// </code>
/// </para>
/// </remarks>
/// <typeparam name="TReadModel">The type of read model under test.</typeparam>
public class ReadModelScenario<TReadModel>
    where TReadModel : class
{
    readonly TReadModel? _initialState;
    readonly INamingPolicy _namingPolicy;
    readonly IEventTypes _eventTypes;
    readonly IClientArtifactsProvider _clientArtifactsProvider;
    readonly IClientArtifactsActivator _artifactsActivator;
    readonly JsonSerializerOptions _jsonSerializerOptions;
    TReadModel? _instance;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadModelScenario{TReadModel}"/> class.
    /// </summary>
    /// <param name="initialState">Optional initial state for the read model before any events are applied.</param>
    public ReadModelScenario(TReadModel? initialState = null)
        : this(initialState, Defaults.Instance)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadModelScenario{TReadModel}"/> class with a custom set of defaults.
    /// </summary>
    /// <param name="initialState">Optional initial state for the read model before any events are applied.</param>
    /// <param name="defaults">The <see cref="Defaults"/> to use for service resolution.</param>
    public ReadModelScenario(TReadModel? initialState, Defaults defaults)
    {
        _initialState = initialState;
        _namingPolicy = new CamelCaseNamingPolicy();
        _eventTypes = defaults.EventTypes;
        _clientArtifactsProvider = defaults.ClientArtifactsProvider;
        _artifactsActivator = new ClientArtifactsActivator(new DefaultServiceProvider(), new NullLoggerFactory());
        _jsonSerializerOptions = Globals.JsonSerializerOptions;
    }

    /// <summary>
    /// Gets the current projected read model instance.
    /// </summary>
    /// <remarks>
    /// This property returns <c>null</c> if <see cref="Given(object[])"/> has not been called yet or if
    /// the events produced no state changes.
    /// </remarks>
    public TReadModel? Instance => _instance;

    /// <summary>
    /// Feeds the provided events through the read model's projection or reducer and updates <see cref="Instance"/>.
    /// </summary>
    /// <remarks>
    /// This method blocks synchronously using <c>GetAwaiter().GetResult()</c>. It is safe to call from
    /// test code running under xUnit, Mocha, or similar frameworks that do not install a synchronization
    /// context that could cause a deadlock.
    /// </remarks>
    /// <param name="events">The event instances to process in order.</param>
    public void Given(params object[] events)
    {
        _instance = GivenAsync(events).GetAwaiter().GetResult();
    }

    async Task<TReadModel?> GivenAsync(IEnumerable<object> events)
    {
        var readModelType = typeof(TReadModel);

        var reducerType = FindReducerType(readModelType);
        if (reducerType is not null)
        {
            return await ReducerReadModelProcessor.Process<TReadModel>(
                reducerType,
                events.Select(e => new EventForEventSourceId(EventSourceId.New(), e, Causation.Unknown())),
                _eventTypes,
                _artifactsActivator,
                new DefaultServiceProvider(),
                _namingPolicy);
        }

        var projectionDefinition = FindProjectionDefinition(readModelType);
        if (projectionDefinition is not null)
        {
            return await ProjectionReadModelProcessor.Process<TReadModel>(
                projectionDefinition,
                events,
                _eventTypes,
                Defaults.Instance.JsonSchemaGenerator,
                _initialState);
        }

        throw new NoReadModelHandlerFound(readModelType);
    }

    Type? FindReducerType(Type readModelType) =>
        _clientArtifactsProvider.Reducers.FirstOrDefault(t =>
            t.GetInterfaces().Any(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(IReducerFor<>) &&
                i.GetGenericArguments()[0] == readModelType));

    Contracts.Projections.ProjectionDefinition? FindProjectionDefinition(Type readModelType)
    {
        // Try fluent IProjectionFor<TReadModel> projection
        var projectionType = _clientArtifactsProvider.Projections.FirstOrDefault(t =>
            t.GetInterfaces().Any(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(IProjectionFor<>) &&
                i.GetGenericArguments()[0] == readModelType));

        if (projectionType is not null)
        {
            return BuildFluentProjectionDefinition(projectionType, readModelType);
        }

        // Try model-bound projection directly on TReadModel
        if (readModelType.HasModelBoundProjectionAttributes())
        {
            var builder = new ModelBoundProjectionBuilder(_namingPolicy, _eventTypes);
            return builder.Build(readModelType);
        }

        // Try model-bound projection for a type in clientArtifacts that matches
        var modelBoundType = _clientArtifactsProvider.ModelBoundProjections
            .FirstOrDefault(t => t == readModelType);

        if (modelBoundType is not null)
        {
            var builder = new ModelBoundProjectionBuilder(_namingPolicy, _eventTypes);
            return builder.Build(modelBoundType);
        }

        return null;
    }

    Contracts.Projections.ProjectionDefinition? BuildFluentProjectionDefinition(Type projectionType, Type readModelType)
    {
        var creatorType = typeof(ProjectionDefinitionCreator<>).MakeGenericType(readModelType);
        var method = creatorType.GetMethod(nameof(ProjectionDefinitionCreator<TReadModel>.CreateAndDefine), BindingFlags.Public | BindingFlags.Static)
            ?? throw new ProjectionDefinitionBuildFailed(projectionType, new InvalidOperationException("CreateAndDefine method not found on ProjectionDefinitionCreator."));

        var result = (Cratis.Monads.Catch<Contracts.Projections.ProjectionDefinition>)method.Invoke(
            null,
            [
                projectionType,
                _namingPolicy,
                _eventTypes,
                _artifactsActivator,
                _jsonSerializerOptions
            ])!;

        if (result.TryGetException(out var exception))
        {
            throw new ProjectionDefinitionBuildFailed(projectionType, exception);
        }

        return result.AsT0;
    }

    static class ProjectionDefinitionCreator<TModel>
        where TModel : class
    {
        /// <summary>
        /// Creates and builds a <see cref="Contracts.Projections.ProjectionDefinition"/> from an <see cref="IProjectionFor{TReadModel}"/> type.
        /// </summary>
        /// <param name="type">The projection type.</param>
        /// <param name="namingPolicy">The <see cref="INamingPolicy"/> to use.</param>
        /// <param name="eventTypes">The <see cref="IEventTypes"/>.</param>
        /// <param name="artifactsActivator">The <see cref="IClientArtifactsActivator"/>.</param>
        /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/>.</param>
        /// <returns>A <see cref="Cratis.Monads.Catch{T}"/> wrapping the built definition.</returns>
        public static Cratis.Monads.Catch<Contracts.Projections.ProjectionDefinition> CreateAndDefine(
            Type type,
            INamingPolicy namingPolicy,
            IEventTypes eventTypes,
            IClientArtifactsActivator artifactsActivator,
            JsonSerializerOptions jsonSerializerOptions)
        {
            try
            {
                var activateResult = artifactsActivator.ActivateNonDisposable<IProjectionFor<TModel>>(type);
                if (activateResult.TryGetException(out var activateException))
                {
                    return activateException;
                }

                var builder = new ProjectionBuilderFor<TModel>(
                    type.GetProjectionId(),
                    type,
                    namingPolicy,
                    eventTypes,
                    jsonSerializerOptions);
                activateResult.AsT0.Define(builder);
                return builder.Build();
            }
            catch (Exception ex)
            {
                return ex;
            }
        }
    }
}
