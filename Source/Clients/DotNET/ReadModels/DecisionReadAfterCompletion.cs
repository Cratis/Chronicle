// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels;

/// <summary>Thrown when a decision read is enrolled after the transaction completed.</summary>
public class DecisionReadAfterCompletion() : Exception("A decision read cannot be enrolled after the unit of work completed.");
