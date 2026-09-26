// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels;

/// <summary>Thrown when a protected read belongs to a different event store, namespace or sequence.</summary>
public class DecisionReadTargetMismatch() : Exception("The decision read targets a different event store, namespace or event sequence.");
