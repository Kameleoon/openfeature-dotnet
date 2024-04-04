using Moq;
using OpenFeature.Constant;
using OpenFeature.Error;
using OpenFeature.Model;
using Xunit;

namespace Kameleoon.OpenFeature.Tests
{
    public class KameleoonProviderTest
    {
        private const string ClientId = "clientId";
        private const string ClientSecret = "clientSecret";
        private const string SiteCode = "siteCode";
        private const string FlagKey = "flagKey";

        private readonly KameleoonClientConfig _config;
        private readonly Mock<IKameleoonClient> _clientMock;
        private readonly Mock<IResolver> _resolverMock;
        private readonly KameleoonProvider _provider;

        public KameleoonProviderTest()
        {
            _config = new KameleoonClientConfig(ClientId, ClientSecret);
            _clientMock = new Mock<IKameleoonClient>();
            _resolverMock = new Mock<IResolver>();
            _provider = new KameleoonProvider(SiteCode, _clientMock.Object, _resolverMock.Object);
        }

        [Fact]
        public void Init_WithInvalidSiteCode_ThrowsFeatureProviderException()
        {
            // Arrange
            string siteCode = ""; // or any invalid site code that will trigger the exception

            // Act & Assert
            Assert.Throws<FeatureProviderException>(() => new KameleoonProvider(siteCode, _config));
        }

        [Fact]
        public void GetMetadata_ReturnsCorrectMetadata()
        {
            // Act
            var metadata = _provider.GetMetadata();

            // Assert
            Assert.Equal("Kameleoon Provider", metadata.Name);
        }

        // Test ResolveBooleanValue, ResolveDoubleValue, ResolveIntegerValue, ResolveStringValue similarly

        private void SetupResolverMock<T>(T defaultValue, T expectedValue)
        {
            _resolverMock.Setup(r => r.Resolve(FlagKey, defaultValue, null))
                        .Returns(new ResolutionDetails<T>(FlagKey, expectedValue, ErrorType.None, null));
        }

        private static void AssertResult<T>(ResolutionDetails<T> result, string flagKey, T expectedValue)
        {
            Assert.Equal(flagKey, result.FlagKey);
            Assert.Equal(expectedValue, result.Value);
            Assert.Equal(ErrorType.None, result.ErrorType);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public async Task ResolveBooleanValue_ReturnsCorrectValue()
        {
            // Arrange
            bool defaultValue = false, expectedValue = true;
            SetupResolverMock(defaultValue, expectedValue);

            // Act
            var result = await _provider.ResolveBooleanValue(FlagKey, defaultValue);

            // Assert
            AssertResult(result, FlagKey, expectedValue);
        }

        [Fact]
        public async Task ResolveDoubleValue_ReturnsCorrectValue()
        {
            // Arrange
            double defaultValue = 0.5, expectedValue = 2.5;
            SetupResolverMock(defaultValue, expectedValue);

            // Act
            var result = await _provider.ResolveDoubleValue(FlagKey, defaultValue);

            // Assert
            AssertResult(result, FlagKey, expectedValue);
        }

        [Fact]
        public async Task ResolveIntegerValue_ReturnsCorrectValue()
        {
            // Arrange
            int defaultValue = 1, expectedValue = 2;
            SetupResolverMock(defaultValue, expectedValue);

            // Act
            var result = await _provider.ResolveIntegerValue(FlagKey, defaultValue);

            // Assert
            AssertResult(result, FlagKey, expectedValue);
        }

        [Fact]
        public async Task ResolveStringValue_ReturnsCorrectValue()
        {
            // Arrange
            string defaultValue = "1", expectedValue = "2";
            SetupResolverMock(defaultValue, expectedValue);

            // Act
            var result = await _provider.ResolveStringValue(FlagKey, defaultValue);

            // Assert
            AssertResult(result, FlagKey, expectedValue);
        }


        [Fact]
        public async Task ResolveStructureValue_ReturnsCorrectValue()
        {
            // Arrange
            var defaultValue = new Value("default");
            var expectedResult = "expected";
            _resolverMock.Setup(r => r.Resolve<object>(FlagKey, defaultValue, null))
                        .Returns(new ResolutionDetails<object>(FlagKey, expectedResult, ErrorType.None, null));

            // Act
            var result = await _provider.ResolveStructureValue(FlagKey, defaultValue, null);

            // Assert
            Assert.Equal(FlagKey, result.FlagKey);
            Assert.Equal(expectedResult, result.Value.AsString);
            Assert.Equal(ErrorType.None, result.ErrorType);
            Assert.Null(result.ErrorMessage);
        }

        public static TheoryData<Task, ProviderStatus> GetStatus_ReturnsProperStatus_Data =
            new()
            {
                { Task.CompletedTask, ProviderStatus.Ready },
                { Task.FromCanceled(new CancellationToken(true)), ProviderStatus.NotReady },
            };


        [Theory]
        [MemberData(nameof(GetStatus_ReturnsProperStatus_Data))]
        public void GetStatus_ReturnsProperStatus(Task providedTask, ProviderStatus expectedStatus)
        {
            // Arrange
            _clientMock.Setup(c => c.WaitInit()).Returns(providedTask);

            // Act
            var status = _provider.GetStatus();

            // Assert
            Assert.Equal(expectedStatus, status);
        }

        [Fact]
        public async Task Initialize_WaitsForClientInitialization()
        {
            // Act
            await _provider.Initialize(EvaluationContext.Builder().Build());

            // Assert
            _clientMock.Verify(c => c.WaitInit(), Times.Once);
        }

        [Fact]
        public async Task Shutdown_ForgetSiteCode()
        {
            // Arrange
            var provider = new KameleoonProvider(SiteCode, _config);
            var clientFirst = provider.Client;
            var clientToCheck = KameleoonClientFactory.Create(SiteCode, _config);

            // Act
            await provider.Shutdown();
            var clientSecond = new KameleoonProvider(SiteCode, _config);

            // Assert
            Assert.Same(clientToCheck, clientFirst);
            Assert.NotSame(clientFirst, clientSecond);
        }
    }
}
