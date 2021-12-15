using Cratis.Common;

namespace Cratis.Events.Projections.for_ProjectionResultStores
{
    public class when_asking_for_known_type : Specification
    {
        static ProjectionResultStoreTypeId type = "df371e5d-b244-48d0-aaad-f298a127dd92";
        ProjectionResultStores stores;
        Mock<IProjectionResultStoreFactory> factory;
        Mock<IProjectionResultStore> store;
        bool result;
        Model model;

        void Establish()
        {
            model = new("Something", null);
            store = new();
            factory = new();
            factory.SetupGet(_ => _.TypeId).Returns(type);
            factory.Setup(_ => _.CreateFor(model)).Returns(store.Object);
            stores = new ProjectionResultStores(new KnownInstancesOf<IProjectionResultStoreFactory>(new[] { factory.Object }));
        }

        void Because() => result = stores.HasType(type);

        [Fact] void should_have_type() => result.ShouldBeTrue();
    }
}
