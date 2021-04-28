using System;
using System.Linq.Expressions;

namespace BlueForest.JsonLogic.Cli
{
    internal class TestSample
    {
        internal string Logic;
        internal object Data;
        internal object TheoricalResult;

        internal TestSample(string json, object data, object theoricalResult)
        {
            this.Logic = json;
            this.Data = data;
            this.TheoricalResult = theoricalResult;
        }
    }

    public class Location
    {
        double _lat, _lon;
        double? _ele;

        Location(double lat=0, double lon=0, double? ele = null)
        {
            _lat = lat;
            _lon = lon;
            _ele = ele;
        }
        public double Latitude { get => _lat; set => _lat = value; }
        public double Longitude { get => _lon; set => _lon = value; }
        public double? Elevation { get => _ele; set => _ele = value; }

    }

    public class TestData
    {
        public int Int;
        public float Float { get; set; }
        public double Double;
        public TestData Nested;
        public Location Location;
    }
    public static class Program
    {
        static int[] ArrayOfIntegers = { 1, 2, 3, 4, 5, 6 };
        static double[] ArrayOfDoubles = { 1, 2, 3, 4, 5, 6 };
        static TestSample[] Samples =
        {
            new TestSample(@"{""var"": 0}",ArrayOfIntegers , 1),
            new TestSample(@"{""var"": 5}",ArrayOfIntegers , 6),
            new TestSample(@"{""var"": [5]}",ArrayOfIntegers , 6),
            new TestSample(@"{""var"": ""Float""}", new TestData(){Float=37.2f } , 37.2f),
            new TestSample(@"{""var"": ""Nested.Int""}", new TestData(){Nested=new TestData(){Int=75 } } , 75),
            new TestSample(@"{""var"": [""Nested.Int""]}", new TestData(){Nested=new TestData(){Int=75 } } , 75),
            new TestSample(@"{""=="": [5, 5] }",null ,true),
            new TestSample(@"{""=="": [5, 10]}",null ,false),
            new TestSample(@"{""=="": [5, {""var"":4}]}",ArrayOfDoubles  ,true),
            new TestSample(@"{""=="": [5, {""var"":0}]}",ArrayOfIntegers  ,false),
            new TestSample(@"{""+"": [5,{""var"":0}]}",ArrayOfIntegers  , 6),
            new TestSample(@"{""min"": [ 1, 2, 3, -12, 5, 6 ]}",null , -12),
            new TestSample(@"{""max"": [ 1, 2, 3, 12, 5, 6 ]}",null , 12),
            new TestSample(@"{""min"": [{""var"":""""}]}",ArrayOfDoubles , 1),
            new TestSample(@"{""max"": [{""var"":""""}]}",ArrayOfIntegers , 6),
            new TestSample(@"{""max"": [{""var"":[""""]}]}",ArrayOfIntegers , 6),
            new TestSample(@"{""some"": [[""banana"", ""orange""],{""=="":[{""var"":""""},""orange""]}]}",null , true),
            new TestSample(@"{""some"": [[1, 2],{""=="":[{""var"":""""},1]}]}",null , true),
            new TestSample(@"{""some"": [[1, 2],{""=="":[{""var"":""""},3]}]}",null , false),
            new TestSample(@"{""some"": [{""var"":""""},{""=="":[{""var"":""""},6]}]}",ArrayOfDoubles , true),
            new TestSample(@"{""all"": [{""var"":""""},{""<="":[{""var"":""""},6]}]}",ArrayOfDoubles , true),
            new TestSample(@"{""all"": [{""var"":""""},{""<"":[{""var"":""""},4]}]}",ArrayOfDoubles , false)

       };
        static void Main(string[] args)
        {
            var sample = Samples[21];
 
            // Compile the lambda expression.
            var compiledExpression = JsonLogic.Compile(sample.Logic, sample.Data?.GetType());
            // Execute the lambda expression.  
            var result = compiledExpression.DynamicInvoke(sample.Data);
            // Display the result.  
            Console.WriteLine(result);
        }
    }
}
/*
var indexVariable = Expression.Variable(typeof(int), "index");
var arrayValue = Expression.Variable(etype, "value");
var result = Expression.Variable(etype, "result");
var lengthExp = Expression.Property(array, typeof(Array).GetPublicInstanceDeclaredOnlyReadProperty(nameof(Array.Length))!);

return Expression.Block(
    new[] { indexVariable, arrayValue, result },
    Expression.Assign(result, Expression.ArrayIndex(array, Expression.Constant(0))),
    ExpressionHelpers.For(
        Expression.Assign(indexVariable, Expression.Constant(0)),
        Expression.LessThan(indexVariable, lengthExp),
        Expression.PostIncrementAssign(indexVariable),
        Expression.Block(
           Expression.Assign(arrayValue, Expression.ArrayIndex(array, indexVariable)),
           Expression.IfThenElse(Expression.LessThan(result, arrayValue),
                                  Expression.Assign(result, op ? arrayValue : result),
                                  Expression.Assign(result, op ? result : arrayValue))
        )
    ),
    result);
*/