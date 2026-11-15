// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Observation.for_Observer.when_removing;

public class and_pruning_ignored_recommendations_fails : given.an_observer
{
    readonly Exception _storageError = new("Could not load recommendations");
    Exception _error;

    void Establish() => _recommendationStorage.GetAll().ThrowsAsync(_storageError);

    async Task Because() => _error = await Catch.Exception(_observer.Remove);

    [Fact] void should_propagate_the_failure() => _error.ShouldEqual(_storageError);
    [Fact] void should_keep_the_observer_record() => _eventStoreNamespaceStorage.Observers.DidNotReceive().Delete(_observerId);
}
