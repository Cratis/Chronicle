// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.SharedTypeCatalog;
using TestAssembly.Catalog;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_ServiceImplementationGenerator.given;

public class a_service_with_body_properties : a_generated_service
{
    const string Converters = """
        namespace TestAssembly.Catalog;
        public static class BodyPropertyConverters
        {
            public static Cratis.Chronicle.SharedTypeCatalog.CoreOwnedValue ToApi(
                this Cratis.Chronicle.Contracts.SharedTypeCatalog.CoreOwnedValue value) => new() { Value = value.Value };
        }
        """;

    protected IReadOnlyDictionary<string, int> _indexes = null!;
    protected Type _requestType = null!;
    protected bool _hasRequest;
    protected int _dispatchCount;

    void Establish()
    {
        SharedTypeRegistry.Configure(2, "Cratis.Chronicle.Contracts");
        _serviceDefinition = new("BodyProperties", typeof(SelectReceipt).Namespace!);
        _serviceDefinition.Commands.Add(new(typeof(RegisterProductWithOptions)));
        _serviceDefinition.Commands.Add(new(typeof(SelectReceipt)));
        _serviceDefinition.Commands.Add(new(typeof(InspectReceipt)));

        // Existing constructor fields keep their wire numbers when body fields are added.
        const string existingContract = """
            public class RegisterProductWithOptionsRequest
            {
                [ProtoMember(7)] public Guid Id { get; set; }
            }
            """;
        File.WriteAllText(Path.Combine(_outputDirectory, "TestAssembly", "Catalog", "IBodyProperties.cs"), existingContract);
    }

    protected async Task<TCommand> GenerateAndDispatch<TCommand>(Action<object>? configure = null)
    {
        var interfaces = new ServiceInterfaceGenerator(0, ContractsNamespace);
        _contractCode = interfaces.Generate(_serviceDefinition, _outputDirectory);
        _indexes = ProtoMemberIndexReader.ReadExistingIndexesFromSource(_contractCode, "RegisterProductWithOptionsRequest");
        var generated = new ServiceImplementationGenerator(0, ContractsNamespace, ImplementationsNamespace)
            .Generate(_serviceDefinition, _outputDirectory);
        var sharedTypes = new ServiceInterfaceGenerator(2, "Cratis.Chronicle.Contracts");
        var (diagnostics, assembly) = GeneratedSourceCompiler.Compile(
            true,
            _contractCode,
            await File.ReadAllTextAsync(generated.Path),
            sharedTypes.GenerateSharedType(typeof(CoreOwnedStatus), _outputDirectory),
            sharedTypes.GenerateSharedType(typeof(CoreOwnedValue), _outputDirectory),
            Converters);
        diagnostics.ShouldBeEmpty();

        var pipeline = Substitute.For<ICommandPipeline>();
        var implementation = assembly!.GetType(generated.TypeName.Replace("global::", string.Empty, StringComparison.Ordinal))!;
        var instance = Activator.CreateInstance(implementation, pipeline)!;
        var method = implementation.GetMethod(typeof(TCommand).Name)!;
        _hasRequest = method.GetParameters().Length == 2;
        object?[] arguments = [default(ProtoBuf.Grpc.CallContext)];
        if (_hasRequest)
        {
            _requestType = method.GetParameters()[0].ParameterType;
            var request = Activator.CreateInstance(_requestType)!;
            configure?.Invoke(request);
            arguments = [request, default(ProtoBuf.Grpc.CallContext)];
        }

        await (Task)method.Invoke(instance, arguments)!;
        var calls = pipeline.ReceivedCalls().ToArray();
        _dispatchCount = calls.Length;
        return (TCommand)calls.Single().GetArguments()[0]!;
    }

    protected static void Set(object request, string name, object? value) => request.GetType().GetProperty(name)!.SetValue(request, value);

    void Destroy() => SharedTypeRegistry.Configure(0, string.Empty);
}
