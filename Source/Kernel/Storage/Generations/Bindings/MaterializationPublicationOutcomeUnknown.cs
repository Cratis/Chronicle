// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Generations.Bindings;

/// <summary>
/// Represents the materialization publication outcome unknown.
/// </summary>
/// <param name="cause">The cause.</param>
public class MaterializationPublicationOutcomeUnknown(Exception cause) : Exception("The publication outcome is unknown. Retry with the same receipt.", cause);
