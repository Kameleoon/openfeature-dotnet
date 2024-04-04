namespace Kameleoon.OpenFeature
{
    /// <summary>
    /// Data type which should be used for configuring the <see cref="EvaluationContext"/> for
    /// </summary>
    public static class Data
    {
        public static class Type
        {
            public const string Conversion = "conversion";
            public const string CustomData = "customData";
        }

        public static class CustomDataType
        {
            public const string Index = "index";
            public const string Values = "values";
        }

        public static class ConversionType
        {
            public const string GoalId = "goalId";
            public const string Revenue = "revenue";
        }
    }
}
