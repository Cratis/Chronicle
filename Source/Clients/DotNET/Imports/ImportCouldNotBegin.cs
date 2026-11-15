// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Imports;

/// <summary>
/// The exception that is thrown when the kernel refuses to begin or reattach an import.
/// </summary>
/// <param name="reason">Why.</param>
public class ImportCouldNotBegin(string reason) : Exception(reason);
