// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

using ReadModelsConfiguration = Cratis.Chronicle.Configuration.ReadModels;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver;

public class when_configuring_batch_size : Specification
{
    Exception? _zeroError;
    Exception? _negativeError;
    int _default;

    void Because()
    {
        _default = new ReadModelsConfiguration().MaxBatchSize;
        _zeroError = Catch.Exception(() => _ = new ReadModelsConfiguration { MaxBatchSize = 0 });
        _negativeError = Catch.Exception(() => _ = new ReadModelsConfiguration { MaxBatchSize = -1 });
    }

    [Fact] void should_default_to_five_hundred() => _default.ShouldEqual(500);
    [Fact] void should_refuse_zero() => _zeroError.ShouldBeOfExactType<ValidationException>();
    [Fact] void should_refuse_a_negative_size() => _negativeError.ShouldBeOfExactType<ValidationException>();
}
