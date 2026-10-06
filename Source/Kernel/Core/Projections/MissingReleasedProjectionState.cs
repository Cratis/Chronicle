// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections;

/// <summary>
/// The exception that is thrown when a protected projection notification has no explicitly released snapshot.
/// </summary>
public class MissingReleasedProjectionState() : Exception("The projection pipeline did not provide a released snapshot for its protected read model notification.");
