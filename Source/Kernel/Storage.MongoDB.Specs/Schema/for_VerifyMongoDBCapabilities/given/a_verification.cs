// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.MongoDB.Schema.for_VerifyMongoDBCapabilities.given;

public class a_verification : Specification
{
    protected MongoDBCapabilityReport _report;
    protected VerifyMongoDBCapabilities _task;
    protected ILogger<VerifyMongoDBCapabilities> _logger;
    protected Exception? _error;
    protected int _inspections;

    void Establish()
    {
        _report = new(MongoDBCapability.Supported, MongoDBCapability.Supported);
        _logger = Substitute.For<ILogger<VerifyMongoDBCapabilities>>();
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        _task = new(
            _ =>
            {
                _inspections++;
                return Task.FromResult(_report);
            },
            _logger);
    }
}
