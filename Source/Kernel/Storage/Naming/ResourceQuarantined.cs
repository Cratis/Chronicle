// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Naming;

/// <summary>
/// The exception that is thrown when an event store or namespace is quarantined because its storage overlaps another one.
/// </summary>
public class ResourceQuarantined() : Exception(ResourceNames.QuarantinedMessage);
