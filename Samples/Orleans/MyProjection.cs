using Cratis.Chronicle.Projections;

namespace Orleans;

public class MyProjection : IProjectionFor<MyModel>
{
    public ProjectionId Identifier => "6f1c7e20-4f22-427d-b02a-2b5df622a13c";

    public void Define(IProjectionBuilderFor<MyModel> builder) => builder
        .From<MyFirstEvent>(_ => _
            .Set(m => m.Name).To(e => e.Name));
}
