namespace Roslyn.Extensions.Metrics;

public class MetricsTemplateData
{
    public string Namespace { get; set; } = string.Empty;

    public string ClassName { get; set; } = string.Empty;

    public List<CounterTemplateData> Counters { get; set; } = new();
}