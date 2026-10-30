// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// A detached native composite value and its seeded state.
/// </summary>
/// <typeparam name="TReadModel">The seeded read model type.</typeparam>
/// <param name="Key">The detached native key.</param>
/// <param name="Identity">The sink identity exposed by the existing instance collection.</param>
/// <param name="Instance">The starting state.</param>
internal record NamedReadModelSeed<TReadModel>(ExpandoObject Key, EventSourceId Identity, TReadModel Instance)
    where TReadModel : class;
