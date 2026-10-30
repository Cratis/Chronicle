// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Security.for_AdministratorRoster.given;

public class an_initializing_administrator_roster : an_administrator_roster
{
    protected RecordingLogger _logger;

    protected IEnumerable<string> Errors => _logger.Errors;

    void Establish()
    {
        _logger = new RecordingLogger();
        _roster = new AdministratorRoster(_storage, _grainFactory, _serializer, Options.Create(_options), new ConfigurationBuilder().Build(), _logger);
    }
}
