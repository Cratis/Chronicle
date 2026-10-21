// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias Client;

using System.Reflection;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration.CSharp;
using Cratis.Chronicle.Schemas;
using Cratis.Screenplay;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration.given;

public class generated_order_code : a_projection_to_generate
{
    protected const string MinimalRemoval = """
        projection Order => Proof.Pdl.Order
          from OrderPlaced
          remove with OrderCancelled
        """;

    protected const string ChildJoin = """
        projection Order => Proof.Pdl.Order
          children Lines identified by ProductId
            from ItemAddedToOrder key ProductId
            join Product on ProductId
              with ProductRegistered
                ProductName = Name
        """;

    protected const string FullOrder = """
        projection Order => Proof.Pdl.Order
          every
            LastUpdated = $eventContext.occurred
          from OrderPlaced
            CustomerName = CustomerName
            IsShipped = false
          from OrderShipped
            IsShipped = true
          from ItemAddedToOrder
            increment ItemCount
          from ItemRemovedFromOrder
            decrement ItemCount
          from ItemQuantityAdjusted
          children Lines identified by ProductId
            from ItemAddedToOrder key ProductId
              parent $eventContext.eventSourceId
              Quantity = Quantity
            from ItemQuantityAdjusted key ProductId
              parent $eventContext.eventSourceId
              Quantity = Quantity
            remove with ItemRemovedFromOrder key ProductId
              parent $eventContext.eventSourceId
            join Product on ProductId
              with ProductRegistered
                ProductName = Name
              with ProductRenamed
                ProductName = Name
          remove with OrderCancelled
        """;

    protected const string NestedAndChildEvery = """
        projection Order => Proof.Pdl.Order
          from OrderPlaced
          nested Details
            from OrderPlaced
              CustomerName = CustomerName
            clear with OrderCancelled
          children Lines identified by ProductId
            from ItemAddedToOrder key ProductId
            every
              LastUpdated = $eventContext.occurred
            remove via join on ItemRemovedFromOrder key ProductId
        """;

    const string Events = """
        [Cratis.Chronicle.Events.EventTypeAttribute]
        public record OrderPlaced(string CustomerName);
        [Cratis.Chronicle.Events.EventTypeAttribute]
        public record OrderShipped;
        [Cratis.Chronicle.Events.EventTypeAttribute]
        public record OrderCancelled;
        [Cratis.Chronicle.Events.EventTypeAttribute]
        public record ItemAddedToOrder(System.Guid ProductId, int Quantity);
        [Cratis.Chronicle.Events.EventTypeAttribute]
        public record ItemQuantityAdjusted(System.Guid ProductId, int Quantity);
        [Cratis.Chronicle.Events.EventTypeAttribute]
        public record ItemRemovedFromOrder(System.Guid ProductId);
        [Cratis.Chronicle.Events.EventTypeAttribute]
        public record ProductRegistered(string Name);
        [Cratis.Chronicle.Events.EventTypeAttribute]
        public record ProductRenamed(string Name);
        """;

    void Establish()
    {
        var lines = JsonSchema.FromJsonAsync("""
            { "type": "array", "items": { "title": "OrderLine", "type": "object", "properties": {
                "ProductId": { "type": "string", "format": "uuid" },
                "ProductName": { "type": "string" }, "Quantity": { "type": "integer" },
                "LastUpdated": { "type": "string", "format": "date-time" }
            } } }
            """).GetAwaiter().GetResult();
        _readModelDefinition = CreateReadModelDefinition("Order", new()
        {
            ["Id"] = new JsonSchemaProperty { Type = JsonObjectType.String, Format = "uuid" },
            ["CustomerName"] = new JsonSchemaProperty { Type = JsonObjectType.String },
            ["IsShipped"] = new JsonSchemaProperty { Type = JsonObjectType.Boolean },
            ["ItemCount"] = new JsonSchemaProperty { Type = JsonObjectType.Integer },
            ["LastUpdated"] = new JsonSchemaProperty { Type = JsonObjectType.String, Format = "date-time" },
            ["Lines"] = new JsonSchemaProperty { Type = JsonObjectType.Array, Item = lines.Item },
            ["Details"] = new JsonSchemaProperty
            {
                Type = JsonObjectType.Object,
                Title = "OrderDetails",
                Properties = { ["CustomerName"] = new JsonSchemaProperty { Type = JsonObjectType.String } }
            }
        });
    }

    protected ProjectionDefinition CompileDeclaration(string declaration)
    {
        var result = new ScreenplayCompiler().CompileProjection(declaration);
        result.Diagnostics.Where(d => d.Severity == Cratis.Screenplay.Diagnostics.DiagnosticSeverity.Error).ShouldBeEmpty();
        return new ProjectionDefinitionSyntaxVisitor(ProjectionOwner.Client).Visit(result.Value!);
    }

    protected string GenerateDeclarative(string declaration) => new DeclarativeCodeGenerator().Generate(CompileDeclaration(declaration), _readModelDefinition).ToFullString();

    protected string GenerateModelBound(string declaration) => new ModelBoundCodeGenerator().Generate(CompileDeclaration(declaration), _readModelDefinition).ToFullString();

    protected Assembly CompileGenerated(string declarative, string modelBound, string events = Events)
    {
        var assemblies = new[]
        {
            typeof(Client::Cratis.Chronicle.Projections.IProjectionFor<>).Assembly.Location,
            typeof(Cratis.Concepts.ConceptAs<>).Assembly.Location,
            typeof(Contracts.Projections.ProjectionDefinition).Assembly.Location
        };
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Concat(assemblies).Distinct().Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create(
            $"GeneratedOrder_{Guid.NewGuid():N}",
            [CSharpSyntaxTree.ParseText(declarative), CSharpSyntaxTree.ParseText(modelBound), CSharpSyntaxTree.ParseText(events)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var output = new MemoryStream();
        compilation.Emit(output).Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.ToString()).ShouldBeEmpty();
        return Assembly.Load(output.ToArray());
    }

    protected void ShouldCompile(string declarative, string modelBound)
    {
        var assembly = CompileGenerated(declarative, modelBound);
        generated_projection_definitions.Declarative(assembly).ShouldNotBeNull();
        generated_projection_definitions.ModelBound(assembly).ShouldNotBeNull();
    }
}
