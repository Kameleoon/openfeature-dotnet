using Xunit;

namespace Kameleoon.OpenFeature.Tests
{
    public class TypesTest
    {
        [Fact]
        public void Check_TypeValues_ProverValues()
        {
            // Assert
            Assert.Equal("conversion", Data.Type.Conversion);
            Assert.Equal("customData", Data.Type.CustomData);

            Assert.Equal("index", Data.CustomDataType.Index);
            Assert.Equal("values", Data.CustomDataType.Values);

            Assert.Equal("goalId", Data.ConversionType.GoalId);
            Assert.Equal("revenue", Data.ConversionType.Revenue);
        }
    }
}
