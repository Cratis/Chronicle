// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.Slices;

/// <summary>
/// Raised when a slice is run again or receives inputs after execution has started.
/// </summary>
public class SliceAlreadyRun() : Exception("The slice has already started. Create a new slice to run another scenario.");
