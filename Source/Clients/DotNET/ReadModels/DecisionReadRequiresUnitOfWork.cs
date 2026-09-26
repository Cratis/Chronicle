// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels;

/// <summary>Thrown when the ambient unit of work does not exist.</summary>
public class DecisionReadRequiresUnitOfWork() : Exception("A decision read requires an ambient unit of work.");
