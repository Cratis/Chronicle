// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Represents Chronicle-specific MongoDB storage options.
/// </summary>
public class MongoDBStorageOptions
{
    /// <summary>
    /// Gets or sets the prefix prepended to every database name. Empty preserves the default names.
    /// </summary>
    public string DatabaseNamePrefix { get; set; } = string.Empty;
}
