// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_TypedEventSourceId;

public record DecimalConcept(decimal Value) : ConceptAs<decimal>(Value);
