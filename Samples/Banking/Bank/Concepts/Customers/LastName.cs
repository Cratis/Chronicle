// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Concepts.Customers;

public record LastName(string Value) : ConceptAs<string>(Value);
