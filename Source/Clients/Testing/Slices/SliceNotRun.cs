// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.Slices;

/// <summary>
/// Raised when an outcome is requested before successful slice completion.
/// </summary>
public class SliceNotRun() : Exception("The slice has not completed successfully. Await Run before accessing Then.");
