// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Validates the storage configuration, with messages that name the setting to change.
/// </summary>
static class StorageValidation
{
    const string ConnectionDetailsMessage =
        "Storage:ConnectionDetails is required. Set the Cratis__Chronicle__Storage__ConnectionDetails environment variable (or Cratis:Chronicle:Storage:ConnectionDetails, or Storage:ConnectionDetailsFile).";

    const string TypeMessage =
        $"Storage:Type could not be inferred from the connection details. Set the Cratis__Chronicle__Storage__Type environment variable (or Cratis:Chronicle:Storage:Type) to one of: {StorageType.MongoDB}, {StorageType.PostgreSql}, {StorageType.MsSql}, {StorageType.Sqlite}, {StorageType.InMemory}.";

    /// <summary>
    /// Validates the storage configuration.
    /// </summary>
    /// <param name="storage">The <see cref="Storage"/> configuration.</param>
    /// <returns>Zero or more validation errors.</returns>
    internal static IEnumerable<ValidationResult> Validate(Storage storage)
    {
        if (string.Equals(storage.Type, StorageType.InMemory, StringComparison.OrdinalIgnoreCase))
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(storage.ConnectionDetails))
        {
            if (!string.IsNullOrWhiteSpace(storage.ConnectionDetailsFile))
            {
                // The file is read into ConnectionDetails before binding; it satisfies the requirement.
                yield break;
            }

            yield return new ValidationResult(ConnectionDetailsMessage, [$"{nameof(ChronicleOptions.Storage)}.{nameof(Storage.ConnectionDetails)}"]);
        }
        else if (string.IsNullOrWhiteSpace(storage.Type) && StorageTypeInference.Infer(storage.ConnectionDetails) is null)
        {
            yield return new ValidationResult(TypeMessage, [$"{nameof(ChronicleOptions.Storage)}.{nameof(Storage.Type)}"]);
        }
    }
}
