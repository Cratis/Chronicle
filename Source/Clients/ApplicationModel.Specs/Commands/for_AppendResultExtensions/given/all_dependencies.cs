// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Applications.Commands.for_AppendResultExtensions.given;

public class all_dependencies : Specification
{
    protected CorrelationId _correlationId;

    void Establish()
    {
        _correlationId = Guid.NewGuid();
    }
}
