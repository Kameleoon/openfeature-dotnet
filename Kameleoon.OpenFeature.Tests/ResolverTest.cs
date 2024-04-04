using Kameleoon.OpenFeature.Utils;
using Moq;
using OpenFeature.Constant;
using OpenFeature.Model;
using Xunit;

namespace Kameleoon.OpenFeature.Tests
{
    public class ResolverTest
    {
        [Fact]
        public void Resolve_WithNullContext_ReturnsErrorForMissingTargetingKey()
        {
            // Arrange
            var clientMock = new Mock<IKameleoonClient>();
            var resolver = new Resolver(clientMock.Object);
            string flagKey = "testFlag";
            string defaultValue = "defaultValue";

            // Act
            var result = resolver.Resolve(flagKey, defaultValue, context: null);

            // Assert
            Assert.Equal(defaultValue, result.Value);
            Assert.Equal(ErrorType.TargetingKeyMissing, result.ErrorType);
            Assert.Equal("The TargetingKey is required in context and cannot be ommited.", result.ErrorMessage);
            Assert.Null(result.Variant);
        }

        public static IEnumerable<object?[]> Resolve_NoMatchVariables_ReturnsErrorForFlagNotFound_DataProvider()
        {
            var variant = "on";
            yield return new object[] { variant, false, new Dictionary<string, object> { },
                    $"The variation '{variant}' has no variables" };
            variant = "var";
            yield return new object[] { variant, true, new Dictionary<string, object> { { "key", new object()} },
                    $"The value for provided variable key 'variableKey' isn't found in variation '{variant}'"
            };
        }

        [Theory]
        [MemberData(nameof(Resolve_NoMatchVariables_ReturnsErrorForFlagNotFound_DataProvider))]
        public void Resolve_NoMatchVariable_ReturnsErrorForFlagNotFound(
            string variant, bool addVariableKey, Dictionary<string, object> variables, string expectedErrorMessage)
        {
            // Arrange
            var clientMock = new Mock<IKameleoonClient>();
            clientMock
                .Setup(m => m.GetFeatureVariationKey(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(variant);
            clientMock
                .Setup(m => m.GetFeatureVariationVariables(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(variables);
            var resolver = new Resolver(clientMock.Object);
            string flagKey = "testFlag";
            int defaultValue = 42;
            var contextBuilder = EvaluationContext.Builder().SetTargetingKey("testVisitor");
            if (addVariableKey)
                contextBuilder.Set(Variable.VariableKey, "variableKey");
            var context = contextBuilder.Build();

            // Act
            var result = resolver.Resolve(flagKey, defaultValue, context);

            // Assert
            Assert.Equal(defaultValue, result.Value);
            Assert.Equal(ErrorType.FlagNotFound, result.ErrorType);
            Assert.Equal(expectedErrorMessage, result.ErrorMessage);
            Assert.Equal(variant, result.Variant);
        }

        [Theory]
        [InlineData(true)]
        [InlineData("string")]
        [InlineData(10.0)]
        public void Resolve_MismatchType_ReturnsErrorTypeMismatch(object returnValue)
        {
            // Arrange
            var variant = "on";
            var clientMock = new Mock<IKameleoonClient>();
            clientMock
                .Setup(m => m.GetFeatureVariationKey(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(variant);
            clientMock
                .Setup(m => m.GetFeatureVariationVariables(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new Dictionary<string, object> { { "key", returnValue } });
            var resolver = new Resolver(clientMock.Object);
            string flagKey = "testFlag";
            int defaultValue = 42;
            var contextBuilder = EvaluationContext.Builder().SetTargetingKey("testVisitor");
            var context = contextBuilder.Build();

            // Act
            var result = resolver.Resolve(flagKey, defaultValue, context);

            // Assert
            Assert.Equal(defaultValue, result.Value);
            Assert.Equal(ErrorType.TypeMismatch, result.ErrorType);
            Assert.Equal("The type of value received is different from the requested value.", result.ErrorMessage);
            Assert.Equal(variant, result.Variant);
        }

        public static TheoryData<KameleoonException, ErrorType> KameleoonException_Data =
            new()
            {
                { new KameleoonException.FeatureException("featureException"), ErrorType.FlagNotFound },
                { new KameleoonException.VisitorCodeInvalid("visitorCodeInvalid"), ErrorType.InvalidContext },
            };

        [Theory]
        [MemberData(nameof(KameleoonException_Data))]
        public void Resolve_KameleoonException_ReturnsErrorProperError(KameleoonException exception, ErrorType errorType)
        {
            // Arrange
            var clientMock = new Mock<IKameleoonClient>();
            clientMock
                .Setup(m => m.GetFeatureVariationKey(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Throws(exception);
            var resolver = new Resolver(clientMock.Object);
            string flagKey = "testFlag";
            int defaultValue = 42;
            var contextBuilder = EvaluationContext.Builder().SetTargetingKey("testVisitor");
            var context = contextBuilder.Build();

            // Act
            var result = resolver.Resolve(flagKey, defaultValue, context);

            // Assert
            Assert.Equal(defaultValue, result.Value);
            Assert.Equal(errorType, result.ErrorType);
            Assert.Equal(exception.Message, result.ErrorMessage);
            Assert.Null(result.Variant);
        }

        public static IEnumerable<object?[]> Resolve_ReturnsResultDetails_DataProvider()
        {
            yield return new object?[] { null, new Dictionary<string, object> { { "k", 10 } }, 10, 9 };
            yield return new object?[] { null, new Dictionary<string, object> { { "k1", "str" } }, "str", "st" };
            yield return new object?[] { null, new Dictionary<string, object> { { "k2", true } }, true, false };
            yield return new object?[] { null, new Dictionary<string, object> { { "k3", 10.0 } }, 10.0, 11.0 };
            yield return new object[] { "varKey", new Dictionary<string, object> { { "varKey", 10.0 } }, 10.0, 11.0 };
        }

        [Theory]
        [MemberData(nameof(Resolve_ReturnsResultDetails_DataProvider))]
        public void Resolve_ReturnsResultDetails(
                string? variableKey, Dictionary<string, object> variables, object expectedValue, object defaultValue)
        {
            // Arrange
            var variant = "variant";
            var clientMock = new Mock<IKameleoonClient>();
            clientMock
                .Setup(m => m.GetFeatureVariationKey(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(variant);
            clientMock
                .Setup(m => m.GetFeatureVariationVariables(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(variables);
            var resolver = new Resolver(clientMock.Object);
            string flagKey = "testFlag";
            var contextBuilder = EvaluationContext.Builder().SetTargetingKey("testVisitor");
            if (variableKey != null)
                contextBuilder.Set(Variable.VariableKey, variableKey);
            var context = contextBuilder.Build();

            // Act
            var result = resolver.Resolve(flagKey, defaultValue, context);

            // Assert
            Assert.Equal(expectedValue, result.Value);
            Assert.Equal(ErrorType.None, result.ErrorType);
            Assert.Null(result.ErrorMessage);
            Assert.Equal(variant, result.Variant);
        }
    }
}
