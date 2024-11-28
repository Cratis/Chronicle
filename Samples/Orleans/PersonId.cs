// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Orleans;

public record PersonId(Guid Value) : ConceptAs<Guid>(Value);
