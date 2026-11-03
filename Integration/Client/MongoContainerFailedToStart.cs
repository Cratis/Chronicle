// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration;

/// <summary>
/// Exception that is thrown when the out-of-process MongoDB container used by the integration fixture fails to start.
/// </summary>
/// <param name="message">The message, including the collected container diagnostics.</param>
/// <param name="innerException">The original start failure.</param>
public class MongoContainerFailedToStart(string message, Exception innerException) : Exception(message, innerException);
