using Cratis.Chronicle.Projections;

namespace Orleans;

public class MyProjection : IProjectionFor<MyModel>
{
    public void Define(IProjectionBuilderFor<MyModel> builder) => builder
        .From<MyFirstEvent>(_ => _
            .Set(m => m.Name).To(e => e.Name));
}
