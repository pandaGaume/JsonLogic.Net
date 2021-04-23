using System;
using System.Linq.Expressions;

namespace BlueForest.JsonLogic.Cli
{
    public class Data
    {
        readonly int _a;
        readonly int _b;
        readonly Data _n;

        public Data(int a, int b=0, Data nested = null)
        {
            _a = a;
            _b = b;
            _n = nested;
        }
        public int A => _a;
        public int B => _b;

        public Data Nested => _n;
    }

    public static class Program
    {
        const string jsonAnd = "{\"and\":[{\"==\":[{\"+\":[{\"var\":\"A\"}, {\"var\":\"B\"}]},4]},{\"==\":[2, 2]}]}";
        const string jsonAdd = "{\"+\":[{\"var\":\"A\"}, {\"var\":\"Nested.B\"}]}";
        static void Main(string[] args)
        {
            // Create a lambda expression. 
            var le = JsonLogic.Parse<Data>(jsonAnd);
            // Compile the lambda expression.
            var compiledExpression = le.Compile();
            // Execute the lambda expression.  
            var result = compiledExpression(new Data(2,3, new Data(1,2)));
            // Display the result.  
            Console.WriteLine(result);
        }
    }
}
