// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// The exception that is thrown when lifecycle handlers have neither a logical activation identity nor PerProcess scope.
/// </summary>
public class ActivationIdRequired() : Exception("Lifecycle handlers require an ActivationId supplied by the host or ActivationScope.PerProcess.");
