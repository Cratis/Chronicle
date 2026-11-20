// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;

namespace Cratis.Chronicle.AspNetCore.for_ChronicleStartup.given;

public abstract class a_failed_startup : a_registered_web_host
{
    protected Exception _failure;

    void Establish() => _app.UseCratisChronicle();
}
