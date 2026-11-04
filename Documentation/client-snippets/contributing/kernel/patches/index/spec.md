```csharp
using Cratis.Specifications;
using NSubstitute;
using Xunit;

public class when_applying_the_patch : Specification
{
    readonly IStorage _storage = Substitute.For<IStorage>();
    readonly ILogger<PatchesBasicPatch> _logger = Substitute.For<ILogger<PatchesBasicPatch>>();
    PatchesBasicPatch _patch = default!;

    void Establish() => _patch = new PatchesBasicPatch(_storage, _logger);

    Task Because() => _patch.Up();

    [Fact] void should_complete_without_throwing() => true.ShouldBeTrue();
}
```
