// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Imports;

/// <summary>
/// The exception that is thrown when importing is not supported by an event sequence implementation.
/// </summary>
public class ImportsNotSupported() : Exception("This event sequence does not support importing events.");
