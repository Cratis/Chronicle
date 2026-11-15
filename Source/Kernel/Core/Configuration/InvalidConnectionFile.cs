// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// The exception that is thrown when a connection setting read from a file, such as <c language="text">storage.connectionDetailsFile</c>, cannot be used.
/// </summary>
/// <remarks>
/// The message names the setting and the path of the file, never what the file holds.
/// </remarks>
/// <param name="setting">The setting that is invalid, for instance <c language="text">storage.connectionDetailsFile</c>.</param>
/// <param name="reason">Why it is invalid.</param>
public class InvalidConnectionFile(string setting, string reason) : Exception($"Connection setting '{setting}' is invalid: {reason}");
