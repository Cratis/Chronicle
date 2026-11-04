// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// The exception that is thrown when a write to a projection marked for applied event markers contains an update operation that cannot be expressed as a guarded pipeline update.
/// </summary>
/// <param name="operation">The operation or path that is not supported.</param>
public class AppliedEventMarkerUpdateNotSupported(string operation) : Exception($"The update '{operation}' cannot be written as a guarded applied event marker update. Only plain property sets and unsets are supported for projections marked for applied event markers.");
