// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle;

/// <summary>
/// The exception that is thrown when client artifact discovery accesses artifacts on the initializing thread before initialization completes.
/// </summary>
public class ReentrantClientArtifactsInitialization()
    : Exception("Client artifacts cannot be accessed reentrantly during initialization. Complete assembly discovery before accessing client artifacts.");
