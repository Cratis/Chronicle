// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.Slices;

/// <summary>
/// Raised when an operation is requested from a disposed slice.
/// </summary>
public class SliceDisposed() : Exception("The slice has been disposed.");
