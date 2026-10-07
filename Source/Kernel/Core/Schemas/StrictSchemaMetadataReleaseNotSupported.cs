// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// The exception that is thrown when a metadata manager cannot release content safely for persistence.
/// </summary>
public class StrictSchemaMetadataReleaseNotSupported() : Exception("Strict schema metadata release is required at the persistence boundary");
