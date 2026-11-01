// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// The exception that is thrown when an observer's progress could not be handed over to the event sequence it was registered on,
/// because the stored owner kept changing.
/// </summary>
/// <param name="observerKey">The key the observer was registered with.</param>
public class ObserverSequenceHandoverDidNotComplete(ObserverKey observerKey)
    : Exception($"Observer '{observerKey.ObserverId}' could not hand its progress in namespace '{observerKey.Namespace}' over to event sequence '{observerKey.EventSequenceId}' because the stored owner kept changing. Register it again to retry.");
