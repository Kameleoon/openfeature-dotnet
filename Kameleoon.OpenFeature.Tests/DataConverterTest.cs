using Kameleoon.Data;
using Newtonsoft.Json.Linq;
using OpenFeature.Model;
using Xunit;

namespace Kameleoon.OpenFeature.Tests
{
    public class DataConverterTests
    {
        [Fact]
        public void ToKameleoon_NullContext_ReturnsEmpty()
        {
            // Arrange
            EvaluationContext? context = null;

            // Act
            var result = DataConverter.ToKameleoon(context);

            // Assert
            Assert.Empty(result);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ToKameleoon_WithConversionData_ReturnsConversionData(bool addRevenue)
        {
            // Arrange
            var rnd = new Random();
            var expectedGoalId = rnd.Next();
            var expectedRevenue = Helpers.NextFloat(rnd);
            var conversionDictionary =
                    new Dictionary<string, Value> { { Data.ConversionType.GoalId, new Value(expectedGoalId) } };
            if (addRevenue)
                conversionDictionary.Add(Data.ConversionType.Revenue, new Value(expectedRevenue));
            var context = EvaluationContext.Builder()
                .Set(Data.Type.Conversion, new Structure(conversionDictionary))
                .Build();

            // Act
            var result = DataConverter.ToKameleoon(context);

            // Assert
            var data = Assert.Single(result);
            var conversion = Assert.IsType<Conversion>(data);
            Assert.Equal(expectedGoalId, conversion.GoalId);
            if (addRevenue)
                Assert.Equal(expectedRevenue, conversion.Revenue);
        }

        [Theory]
        [InlineData()]
        [InlineData("")]
        [InlineData("v1")]
        [InlineData("v1", "v1")]
        [InlineData("v1", "v2", "v3")]
        public void ToKameleoonData_WithCustomData_ReturnsCustomData(params string[] expectedValues)
        {
            // Arrange
            var expectedIndex = new Random().Next();
            var customDataDictionary =
                    new Dictionary<string, Value> { { Data.CustomDataType.Index, new Value(expectedIndex) } };
            if (expectedValues.Length == 1)
                customDataDictionary.Add(Data.CustomDataType.Values,
                        new Value(expectedValues[0]));
            else if (expectedValues.Length > 1)
                customDataDictionary.Add(Data.CustomDataType.Values,
                        new Value(expectedValues.Select(v => new Value(v)).ToList()));
            var context = EvaluationContext.Builder()
                .Set(Data.Type.CustomData, new Structure(customDataDictionary))
                .Build();

            // Act
            var result = DataConverter.ToKameleoon(context);

            // Assert
            var data = Assert.Single(result);
            var customData = Assert.IsType<CustomData>(data);
            Assert.Equal(expectedIndex, customData.Id);
            Assert.Equal(expectedValues, customData.Values);
        }

        [Fact]
        public void ToKameleoonData_AllTypes_ReturnsAllData()
        {
            // Arrange
            var rnd = new Random();
            var goaldId1 = rnd.Next();
            var goaldId2 = rnd.Next();
            var index1 = rnd.Next();
            var index2 = rnd.Next();
            var context = EvaluationContext.Builder()
                .Set(Data.Type.Conversion, new Value(new Value[] {
                    new(new Structure(
                        new Dictionary<string, Value> { { Data.ConversionType.GoalId, new Value(goaldId1) } }
                    )),
                    new(new Structure(
                        new Dictionary<string, Value> { { Data.ConversionType.GoalId, new Value(goaldId2) } }
                    )),
                }))
                .Set(Data.Type.CustomData, new Value(new Value[] {
                    new(new Structure(
                        new Dictionary<string, Value> { { Data.CustomDataType.Index, new Value(index1) } }
                    )),
                    new(new Structure(
                        new Dictionary<string, Value> { { Data.CustomDataType.Index, new Value(index2) } }
                    )),
                }))
                .Build();

            // Act
            var result = DataConverter.ToKameleoon(context).ToArray();

            // Assert
            Assert.Equal(4, result.Length);
            var conversions = result.OfType<Conversion>().ToArray();
            Assert.Equal(goaldId1, conversions[0].GoalId);
            Assert.Equal(goaldId2, conversions[1].GoalId);
            var customData = result.OfType<CustomData>().ToArray();
            Assert.Equal(index1, customData[0].Id);
            Assert.Equal(index2, customData[1].Id);
        }

        public static IEnumerable<object?[]> TestData =>
            new List<object?[]>
            {
                new object?[] { null, new Value() }, // null value
                new object[] { 42, new Value(42) }, // int value
                new object[] { 3.14, new Value(3.14) }, // double value
                new object[] { true, new Value(true) }, // bool value
                new object[] { "test", new Value("test") }, // string value
                new object[] {
                    JObject.Parse("{\"key\": \"value\"}"),
                    new Value(new Structure(new Dictionary<string, Value>{ { "key", new Value("value") } }))
                }, // JObject value
                new object[] {
                    JArray.Parse("[1, 2, 3]"),
                    new Value(JArray.Parse("[1, 2, 3]").ToObject<List<int>>()!.Select(v => new Value(v)).ToList())
                }, // JArray value - 1
            };

        [Theory]
        [MemberData(nameof(TestData))]
        public void ToOpenFeature_ReturnsCorrectValue(object input, Value expected)
        {
            // Act
            var result = DataConverter.ToOpenFeature(input);

            // Assert
            Assert.True(expected.EqualWith(result));
        }
    }
}
