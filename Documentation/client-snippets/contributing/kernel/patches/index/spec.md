```csharp
using Cratis.Specifications;
using NSubstitute;
using Xunit;

public class when_rolling_back_the_patch : Specification
{
    readonly ILogger<PatchesRollbackPatch> _logger = Substitute.For<ILogger<PatchesRollbackPatch>>();
    PatchesRollbackPatch _patch = default!;

    void Establish() => _patch = new PatchesRollbackPatch(_logger);

    Task Because() => _patch.Down();

    [Fact] void should_complete_without_throwing() => true.ShouldBeTrue();
}
```
