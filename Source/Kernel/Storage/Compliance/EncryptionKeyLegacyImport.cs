// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>The high-water mark for one legacy fence secret.</summary>
/// <param name="Name">The legacy fence name.</param>
/// <param name="Version">The last imported version.</param>
/// <param name="CreatedOn">Its creation time, when supplied by the service.</param>
/// <param name="ClosedAtLifecycle">The lifecycle closed by this import; cleared by database authorization.</param>
/// <param name="SeenVersions">Previously observed versions, including timestamp ties.</param>
public record EncryptionKeyLegacyImport(string Name, string Version, DateTimeOffset? CreatedOn, uint? ClosedAtLifecycle, IReadOnlyList<string> SeenVersions);
