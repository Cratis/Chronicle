// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Validation;

/// <summary>
/// Represents the base type for a validator of a <see cref="ConceptAs{T}"/>.
/// </summary>
/// <typeparam name="T">Type of query.</typeparam>
public class ConceptValidator<T> : DiscoverableValidator<T>;
