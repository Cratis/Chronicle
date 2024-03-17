// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Basic;

public record MaterialId(Guid Value) : ConceptAs<Guid>(Value);
