// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Json.for_ConceptAsJsonConverter;

public record DateOnlyConcept(DateOnly Value) : ConceptAs<DateOnly>(Value);
