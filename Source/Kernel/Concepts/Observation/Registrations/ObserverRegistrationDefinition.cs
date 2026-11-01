// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Cratis.Chronicle.Concepts.Observation.Registrations;

/// <summary>
/// Represents the complete definition persisted with an observer registration.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(ReactorRegistrationDefinition), "reactor")]
[JsonDerivedType(typeof(ReducerRegistrationDefinition), "reducer")]
public abstract record ObserverRegistrationDefinition;
