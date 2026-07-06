// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;
using Cratis.Serialization;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Builds a <see cref="Contracts.Projections.ProjectionDefinition"/> from a standalone fluent
/// <see cref="IProjectionFor{TModel}"/> class.
/// </summary>
/// <typeparam name="TModel">The read model type the projection targets.</typeparam>
/// <remarks>
/// This is a top-level type — not nested inside <see cref="ReadModelScenario{TReadModel}"/> — so that
/// <c>typeof(ProjectionDefinitionCreator&lt;&gt;)</c> has an arity of one. A generic type nested inside another
/// generic type inherits the enclosing type parameters, which would make the open generic arity two and break
/// <see cref="Type.MakeGenericType"/> with a single type argument.
/// </remarks>
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
    /// <returns>A <see cref="Monads.Catch{T}"/> wrapping the built definition.</returns>
    public static Monads.Catch<Contracts.Projections.ProjectionDefinition> CreateAndDefine(
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
        catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException || ex is TargetInvocationException)
        {
            return ex;
        }
    }
}
