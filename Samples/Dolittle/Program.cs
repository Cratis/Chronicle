// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//new HostBuilder().ConfigureWebHostDefaults()
using System.Dynamic;
using System.Linq.Expressions;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Cratis.Events.Projections;
using Sample;

// https://www.c-sharpcorner.com/UploadFile/vendettamit/create-dynamic-linq-using-expressions/
// https://stackoverflow.com/questions/3562088/c-sharp-4-dynamic-in-expression-trees#3591820
// using Microsoft.CSharp.RuntimeBinder;

// https://docs.microsoft.com/en-us/dotnet/api/system.linq.expressions.expression.dynamic?view=net-5.0
// BsonDocument to Dynamic: https://stackoverflow.com/a/13063604
var parameter = Expression.Parameter(typeof(Model));
var expression = Expression.Add(new MyExpression(parameter), new MyExpression(parameter));
var callable = Expression.Lambda<Func<Model, int>>(expression, new[] { parameter }).Compile();
var result = callable(new Model { Value = 43 });
result = callable(new Model { Value = 23 });

var eventA = new EventType("b0ac7a3d-72c8-4bcc-ada6-a0e661df3e0b");
var eventB = new EventType("5cafaa96-79c6-492c-af78-61a691a53ac8");
var eventC = new EventType("8ed7485d-068a-4d20-af0d-247ca622edca");
var eventD = new EventType("c8404d52-7d15-411c-9e10-a01de621756a");

var projection = new Projection(new EventType[] {
    eventA, eventB, eventC
});

projection.Event.Subscribe(_ => Console.WriteLine($"Raw event : {_.Event.Type}"));
projection.Event.From(eventA).Project("A");
projection.Event.From(eventB).Project("B");

projection.OnNext(new Event(new EventLogSequenceNumber(0U), eventC, DateTimeOffset.UtcNow, new EventSourceId(Guid.NewGuid().ToString()), new ExpandoObject()));

// var expando = new ExpandoObject();
// Expression.Dynamic()

// var projection = new Projection();
// projection.From("blah").Project(
//     Expression.Assign(
//         Expression.Property(parameter, "Something"),
//         Expression.Property(parameter, "SomethingElse")
//     )
// );

// var builder = Host.CreateDefaultBuilder()
//                     .UseServiceProviderFactory(new AutofacServiceProviderFactory())
//                     .ConfigureContainer<ContainerBuilder>(containerBuilder => containerBuilder.RegisterDefaults(Startup.Types))
//                     .ConfigureWebHostDefaults(_ => _.UseStartup<Startup>());

// var app = builder.Build();

// app.Run();

namespace Sample
{
    public class Model
    {
        public int Value { get; init; } = 42;
    }

    public class MyExpression : Expression
    {
        readonly ParameterExpression _parameter;

        public MyExpression(ParameterExpression parameter)
        {
            _parameter = parameter;
        }

        public override ExpressionType NodeType => ExpressionType.Extension;

        public override Type Type => typeof(int);

        public override bool CanReduce => true;

        public override Expression Reduce()
        {
            return Property(_parameter, "Value");
        }
    }
}

/*
var builder = WebApplication.CreateBuilder(args)
                            .UseCratis()
                            .UseCratisWorkbench();
var app = builder.Build();
app.AddCratisWorkbench();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.Run();
*/
