// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Schema;

/// <summary>
/// Indicates that a MongoDB-compatible storage service explicitly lacks a required Chronicle feature.
/// </summary>
/// <param name="failures">The unsupported feature names, never connection details or raw server errors.</param>
public class MongoDBCapabilitiesNotSupported(IEnumerable<string> failures) : InvalidOperationException(
    $"The storage service does not support the capabilities Chronicle requires: {string.Join(", ", failures)}. See https://cratis.io/chronicle/hosting/storage-compatibility/");
