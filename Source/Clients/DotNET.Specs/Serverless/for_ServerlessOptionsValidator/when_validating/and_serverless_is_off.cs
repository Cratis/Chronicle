// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Serverless.for_ServerlessOptionsValidator.when_validating;

public class and_serverless_is_off : Specification
{
    ValidateOptionsResult _result;

    void Because() => _result = new ServerlessOptionsValidator<ChronicleOptions>(Substitute.For<IServiceProvider>())
        .Validate(null, new ChronicleOptions());

    [Fact] void should_succeed() => _result.Succeeded.ShouldBeTrue();
}
