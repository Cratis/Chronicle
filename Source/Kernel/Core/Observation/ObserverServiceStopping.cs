// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// The exception that is thrown when work arrives after the observer service has stopped accepting calls.
/// </summary>
public class ObserverServiceStopping() : Exception("The observer service is stopping and cannot accept new work");
