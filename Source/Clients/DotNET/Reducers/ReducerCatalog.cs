// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Registrations;

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Resolves reducer declarations against the owning store's artifact and event registries.
/// </summary>
/// <param name="artifacts">The owning artifact provider.</param>
/// <param name="eventTypes">The owning event registry.</param>
/// <param name="validator">The classic declaration validator.</param>
internal sealed class ReducerCatalog(IClientArtifactsProvider artifacts, IEventTypes eventTypes, IReducerValidator validator)
{
    /// <summary>
    /// Determines whether a non-classic type declares a supported static state transition.
    /// </summary>
    /// <param name="type">The CLR type.</param>
    /// <returns>Whether it is a self reducer.</returns>
    internal static bool IsSelfReducer(Type type) => SelfReducerMethods.HasSelfShape(type);

    /// <summary>
    /// Applies discovery acceptance without rejecting diagnostic-only near-misses.
    /// </summary>
    /// <param name="type">The discovered candidate.</param>
    /// <returns>Whether discovery accepts the candidate.</returns>
    internal bool IsDiscoverable(Type type) =>
        !SelfReducerMethods.IsDiscoveryCandidate(type) || IsSelfReducer(type) ||
        ExplicitArtifactCatalog.From(artifacts)?.IsExplicit(type, ExplicitArtifactKind.Reducer) == true;

    /// <summary>
    /// Resolves runtime nominations using the existing classic instance-model inference.
    /// </summary>
    /// <param name="type">The nominated CLR type.</param>
    /// <returns>The reducer descriptor.</returns>
    internal ReducerDescriptor GetForRegistration(Type type)
    {
        if (IsSelfReducer(type))
        {
            return Get(type);
        }

        SelfReducerMethods.ValidateExplicit(type);
        RegisteredObserverMetadata.Validate(type, ExplicitArtifactKind.Reducer, artifacts);
        return new(type, RegisteredReducerReadModel.Infer(type, eventTypes), false);
    }

    /// <summary>
    /// Resolves the model and declaration style without activating an artifact.
    /// </summary>
    /// <param name="type">The nominated or discovered CLR type.</param>
    /// <returns>The reducer descriptor.</returns>
    internal ReducerDescriptor Get(Type type)
    {
        if (IsSelfReducer(type))
        {
            RegisteredObserverMetadata.Validate(type, ExplicitArtifactKind.Reducer, artifacts);
            return new(type, type, true);
        }

        SelfReducerMethods.ValidateExplicit(type);

        if (ExplicitArtifactCatalog.From(artifacts)?.IsExplicit(type, ExplicitArtifactKind.Reducer) == true || !type.IsAssignableTo(typeof(IReducer)))
        {
            RegisteredObserverMetadata.Validate(type, ExplicitArtifactKind.Reducer, artifacts);
            return new(type, RegisteredReducerReadModel.Infer(type, eventTypes), false);
        }

        validator.Validate(type);
        return new(type, type.GetReadModelType(), false);
    }
}
