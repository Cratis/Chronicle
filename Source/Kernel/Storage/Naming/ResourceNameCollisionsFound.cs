// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Naming;

/// <summary>
/// The exception that is thrown when names overlap in storage and the installation is configured to refuse that.
/// </summary>
/// <param name="count">The number of overlaps found.</param>
public class ResourceNameCollisionsFound(int count) : Exception($"{count} event store or namespace names overlap in storage. See the log for the names involved.");
