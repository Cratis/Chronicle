// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Concepts.for_ConceptFactory;

public record DateOnlyConcept(DateOnly Value) : ConceptAs<DateOnly>(Value);
