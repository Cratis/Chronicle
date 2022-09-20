// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Concepts;

namespace Aksio.Cratis.Json.for_ConceptAsJsonConverter;

public record UIntConcept(uint Value) : ConceptAs<uint>(Value);
