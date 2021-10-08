// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Linq.Expressions;
using System.Reactive.Subjects;
using Cratis.Boot;
using Cratis.Events.Projections;
using Microsoft.CSharp.RuntimeBinder;

namespace Sample
{

    public class ProjectionTesting : IPerformBootProcedure
    {
        const string EventTypeA = "d3e83b35-c11e-46cc-91ca-58cd66d1aa9e";
        const string EventTypeB = "5addd1d0-c270-4d86-87b5-13b7b97b1dde";
        const string EventTypeC = "fe3c32a7-a50d-47ba-bfeb-cf2417453de8";

        class TestProvider : IProjectionEventProvider
        {
            public IObservable<Event> ProvideFor(IProjection projection)
            {
                var subject = new ReplaySubject<Event>();
                subject.OnNext(new Event(0, EventTypeA, DateTimeOffset.UtcNow, "d567f175-f940-4f4d-88ee-d96885a78c1a", new { Integer = 42, String = "Forty Two" }));
                return subject;
            }

            public void Pause(IProjection projection) => throw new NotImplementedException();
            public void Resume(IProjection projection) => throw new NotImplementedException();
            public Task Rewind(IProjection projection) => throw new NotImplementedException();
        }

        public class Something
        {
            public int Integer { get; set; }
        }

        public void Perform()
        {
            var projection = new Projection("08329697-f24a-4890-b1a4-a53b81d0d9ea", new EventType[] {
                EventTypeA, EventTypeB, EventTypeC
            });

            var eventParameter = Expression.Parameter(typeof(Event));
            var targetParameter = Expression.Parameter(typeof(ExpandoObject));

            var integerMember = Binder.GetMember(CSharpBinderFlags.None, "Integer", default, Array.Empty<CSharpArgumentInfo>());
            var contentAccessor = Expression.Property(eventParameter, "Content");

            dynamic instance = new ExpandoObject();
            //instance.Integer = 0;
            //var integerAccessor = Expression.Property(Expression.Constant(instance), "Integer");
            var integerAccessor = Expression.Property(
                Expression.Constant(instance, typeof(IDictionary<string, object>)),
                "Item",
                Expression.Constant("Integer"));

            //Expression.Dynamic(integerMember, typeof(object), contentAccessor);
            /*var dic = new Dictionary<string, object> {
                {"KeyName", 1}
            };*/
            dynamic dic = new ExpandoObject();
            dic.KeyName = 42;

            // Setting
            var objectParameter = Expression.Parameter(typeof(IDictionary<string, object>), "d");
            var propertySetter = Expression.Assign(
                Expression.Property(objectParameter, "Item", Expression.Constant("KeyName")),
                Expression.Constant(43, typeof(object)));

            var setter = Expression.Lambda<Action<IDictionary<string, object>>>(propertySetter, objectParameter).Compile();
            setter(dic);

            // Getting
            var constant = Expression.Constant("KeyName");
            var propertyGetter = Expression.Property(objectParameter, "Item", constant);

            var expr = Expression.Lambda<Func<IDictionary<string, object>, object>>(propertyGetter, objectParameter).Compile();

            Console.WriteLine(expr(dic));

            var increaseExpression = Expression.Assign(
                integerAccessor, Expression.Constant(1));





            var propertyMapper = Expression.Lambda<PropertyMapper>(increaseExpression, new[] {
                eventParameter,
                targetParameter
            }).Compile();
            //projection.Event.From(EventTypeA).Project(propertyMapper);
            var provider = new TestProvider();
            var pipeline = new ProjectionPipeline(provider, projection);
            var storage = new InMemoryProjectionStorage();
            pipeline.StoreIn(storage);
            pipeline.Start();
        }
    }
}
