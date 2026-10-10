// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// The exception that is thrown when a schema generator cannot serve a legacy kernel.
/// </summary>
public sealed class LegacyEventTypeSchemasNotSupported() : Exception("The schema generator does not support legacy event type schemas.");
