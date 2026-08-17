// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.ProtoGenerator;

/// <summary>
/// The outcome of declaring retired field numbers in one generated schema.
/// </summary>
/// <param name="Schema">The schema with the reservations declared.</param>
/// <param name="Declared">The types whose reservations this schema carries.</param>
/// <remarks>
/// One schema is generated per package, and a contract type only has a message in the schemas of the packages whose
/// services reach it. Whether a reservation was emitted is therefore not a question a single schema can answer — the
/// caller has to add these up across every package it generates and check the total.
/// </remarks>
internal record ReservedFieldsDeclaration(string Schema, IReadOnlyCollection<Type> Declared);
