namespace Roslyn.Extensions.Metrics;

public class CounterTemplateData
{
    public string Type { get; set; } = string.Empty;

    public string MethodName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public IEnumerable<CounterTagTemplateData> Tags { get; set; } = Enumerable.Empty<CounterTagTemplateData>();
}
