// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Setup;

/// <summary>
/// Exception that gets thrown when the System event store's data sits under a database name the kernel is not configured to use.
/// </summary>
/// <param name="description">Where the data is and where the kernel looks.</param>
public class SystemEventStoreDataMisplaced(string description)
    : Exception($"{description} See https://cratis.io/chronicle/hosting/configuration/storage/");
