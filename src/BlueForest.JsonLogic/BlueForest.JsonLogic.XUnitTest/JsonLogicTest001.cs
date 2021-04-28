using System;
using Xunit;

namespace BlueForest.JsonLogic.XUnitTest
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

    public class TestData
    {
        public int Int;
        public float Float { get; set; }
        public double Double;
        public TestData Nested;
    }

    public class JsonLogicTest001
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
            new TestSample(@"{""min"": [ 1, 2, 3, 12, 5, 6 ]}",null , 1),
            new TestSample(@"{""max"": [ 1, 2, 3, 12, 5, 6 ]}",null , 12),
            new TestSample(@"{""min"": [{""var"":""""}]}",ArrayOfDoubles , 1),
            new TestSample(@"{""max"": [{""var"":""""}]}",ArrayOfIntegers , 6),
            new TestSample(@"{""max"": [{""var"":[""""]}]}",ArrayOfIntegers , 6),
            new TestSample(@"{""some"": [[""banana"", ""orange""],{""=="":[{""var"":""""},""banana""]}]}",null , true),
            new TestSample(@"{""some"": [[""banana"", ""orange""],{""=="":[{""var"":""""},""apple""]}]}",null , false),
            new TestSample(@"{""some"": [[1, 2],{""=="":[{""var"":""""},1]}]}",null , true),
            new TestSample(@"{""some"": [[1, 2],{""=="":[{""var"":""""},3]}]}",null , false),
            new TestSample(@"{""some"": [{""var"":""""},{""=="":[{""var"":""""},6]}]}",ArrayOfDoubles , true),
            new TestSample(@"{""all"": [{""var"":""""},{""<="":[{""var"":""""},6]}]}",ArrayOfDoubles , true),
            new TestSample(@"{""all"": [{""var"":""""},{""<"":[{""var"":""""},4]}]}",ArrayOfDoubles , false)
        };

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        [InlineData(8)]
        [InlineData(9)]
        [InlineData(10)]
        [InlineData(11)]
        [InlineData(12)]
        [InlineData(13)]
        [InlineData(14)]
        [InlineData(15)]
        [InlineData(16)]
        [InlineData(17)]
        [InlineData(18)]
        [InlineData(19)]
        [InlineData(20)]
        [InlineData(21)]
        public void Test(int testIndex)
        {
            var sample = Samples[testIndex];
            // Create a lambda expression. 
            var lambdaExpr = JsonLogic.Parse(sample.Logic, sample.Data?.GetType());
            // Compile the lambda expression.
            var compiledExpression = lambdaExpr.Compile();
            // Execute the lambda expression.  
            var result = compiledExpression.DynamicInvoke(sample.Data);
            Assert.Equal(Convert.ChangeType(sample.TheoricalResult, result.GetType()), result);
        }
    }
}
