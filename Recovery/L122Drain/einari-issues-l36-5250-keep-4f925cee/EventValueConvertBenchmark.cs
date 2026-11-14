using System.Dynamic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Projections.Engine.Expressions.EventValues;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging.Abstractions;

namespace MicroBench;

public static class Program
{
    public static int Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        return 0;
    }
}

[MemoryDiagnoser]
public class EventValueConvertBenchmark
{
    ValueProvider<AppendedEvent> _scalar = null!;
    ValueProvider<AppendedEvent> _multiMember = null!;
    ValueProvider<AppendedEvent> _singleValue = null!;
    AppendedEvent _event = null!;

    [GlobalSetup]
    public void Setup()
    {
        var resolvers = new EventValueProviderExpressionResolvers(new TypeFormats(), NullLogger<EventValueProviderExpressionResolvers>.Instance);

        var content = new ExpandoObject();
        var dictionary = (IDictionary<string, object?>)content;
        dictionary["plain"] = 42;

        var multi = new ExpandoObject();
        var multiDictionary = (IDictionary<string, object?>)multi;
        multiDictionary["first"] = 1;
        multiDictionary["second"] = "two";
        dictionary["multi"] = multi;

        var single = new ExpandoObject();
        ((IDictionary<string, object?>)single)["value"] = 42;
        dictionary["single"] = single;

        _event = new AppendedEvent(null!, content);

        var integer = new JsonSchemaProperty { Type = JsonObjectType.Integer };
        var objectSchema = new JsonSchemaProperty { Type = JsonObjectType.Object };
        objectSchema.Properties["first"] = new JsonSchemaProperty { Type = JsonObjectType.Integer };
        objectSchema.Properties["second"] = new JsonSchemaProperty { Type = JsonObjectType.String };

        _scalar = resolvers.Resolve(integer, "plain");
        _multiMember = resolvers.Resolve(objectSchema, "multi");
        _singleValue = resolvers.Resolve(integer, "single");
    }

    [Benchmark]
    public object PlainScalar() => _scalar(_event);

    [Benchmark]
    public object MultiMemberObject() => _multiMember(_event);

    [Benchmark]
    public object SingleValueExpando() => _singleValue(_event);
}
