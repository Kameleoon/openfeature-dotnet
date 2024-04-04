using OpenFeature.Constant;
using OpenFeature.Model;
using Xunit;

namespace Kameleoon.OpenFeature.Utils.Tests
{
    public class UtilsTests
    {
        // Variable
        [Fact]
        public void TryGet_WithValidContext_ReturnsVariableValue()
        {
            // Arrange
            var variableValue = "variableValue";
            var context = EvaluationContext.Builder()
                .Set(Variable.VariableKey, new Value(variableValue))
                .Build();

            // Act
            var result = Variable.TryGet(context);

            // Assert
            Assert.Equal(variableValue, result);
        }

        [Fact]
        public void TryGet_WithInvalidContext_ReturnsNull()
        {
            // Arrange
            EvaluationContext? context = null;

            // Act
            var result = Variable.TryGet(context);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void MakeErrorDescription_WithValidValues_ReturnsCorrectDescription()
        {
            // Arrange
            string variant = "testVariant";
            string variableKey = "testVariableKey";

            // Act
            var result = Variable.MakeErrorDescription(variant, variableKey);

            // Assert
            Assert.Equal(
                $"The value for provided variable key '{variableKey}' isn't found in variation '{variant}'",
                result
            );
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void MakeErrorDescription_WithNullVariableKey_ReturnsCorrectDescription(string? variableKey)
        {
            // Arrange
            string variant = "testVariant";

            // Act
            var result = Variable.MakeErrorDescription(variant, variableKey);

            // Assert
            Assert.Equal($"The variation '{variant}' has no variables", result);
        }

        // Extension (KameleoonError)
        [Theory]
        [InlineData(typeof(KameleoonException.FeatureException), ErrorType.FlagNotFound)]
        [InlineData(typeof(KameleoonException.VisitorCodeInvalid), ErrorType.InvalidContext)]
        [InlineData(typeof(KameleoonException.ConfigException), ErrorType.General)]
        public void ToOpenFeatureError_ReturnsCorrectErrorType(Type exceptionType, ErrorType expectedErrorType)
        {
            // Arrange
            var exceptionMessage = "The exception";
            var exception = (KameleoonException)Activator.CreateInstance(exceptionType, exceptionMessage)!;

            // Act
            var (type, message) = exception.ToOpenFeatureError();

            // Assert
            Assert.Equal(expectedErrorType, type);
            Assert.Contains(exceptionMessage, message);
        }


        // Extension (ResolutionDetails)
        [Fact]
        public async Task ResolutionDetails_AsTask()
        {
            // Arrange
            var flagKey = "flagKey";
            var defaultValue = 10;
            var resolutionDetails = new ResolutionDetails<int>(flagKey, defaultValue);

            // Act
            var taskResolutionDetails = resolutionDetails.AsTask();

            // Assert
            Assert.Equal(resolutionDetails, await taskResolutionDetails);
        }
    }
}
