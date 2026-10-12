// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator.when_generating_compensation_schemas;

public class and_clients_generate_concurrently : given.registry_bindings
{
    JsonSchema[] _schemas;

    async Task Because()
    {
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = Task.Run(async () =>
        {
            await start.Task;
            return _generator.Generate(typeof(Compensation), _first);
        });
        var second = Task.Run(async () =>
        {
            await start.Task;
            return _generator.Generate(typeof(Compensation), _second);
        });
        start.SetResult();
        _schemas = await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact] void should_keep_the_first_registry_identity() => _schemas[0].GetCompensationFor().ShouldEqual("first-alias");
    [Fact] void should_keep_the_second_registry_identity() => _schemas[1].GetCompensationFor().ShouldEqual("second-alias");
}
