using System.Threading.Tasks;
using OpenFeature;
using OpenFeature.Model;
using OpenFeature.Error;
using OpenFeature.Constant;
using System.Runtime.CompilerServices;
using Kameleoon.OpenFeature.Utils;

[assembly: InternalsVisibleTo("Kameleoon.OpenFeature.Tests")]

namespace Kameleoon.OpenFeature
{
    /// <summary>
    /// KameleoonProvider is the OpenFeature <see cref="FeatureProvider"/> for Kameleoon SDK.
    /// </summary>
    /// <example>
    ///     var config = new KameleoonClientConfig(clientId: clientId, clientSecret: clientSecret);
    ///     var provider = new KameleoonProvider(siteCode, config);
    ///
    ///     OpenFeature.Api.Instance.SetProvider(provider);
    ///
    ///     var client = OpenFeature.Api.Instance.GetClient();
    /// </example>
    public sealed class KameleoonProvider : FeatureProvider
    {
        /// <summary>
        /// The value Anonymous is used when no Targeted Key provided with the <see cref="EvaluationContext"/>
        /// </summary>
        private const string MetaName = "Kameleoon Provider";

        private readonly string _siteCode;
        private readonly IResolver _resolver;

        /// <summary>
        /// KameleoonClient SDK instance.
        /// <para>
        /// This client instance provides opportunities to leverage additional functionalities beyond those
        /// available in OpenFeature.
        /// </para>
        /// </summary>
        public IKameleoonClient Client { get; }

        /// <summary>
        /// Create a new instance of the provider with the given siteCode and config.
        /// </summary>
        /// <param name="siteCode">Code of the website you want to run experiments on. This unique code id can be found
        /// in our platform's back-office. This field is mandatory.</param>
        /// <param name="config">Configuration SDK object.</param>
        public KameleoonProvider(string siteCode, KameleoonClientConfig config) :
            this(siteCode, MakeKameleoonClient(siteCode, config))
        { }

        /// <summary>
        /// Internal constructor which accepts siteCode and KameleoonClient instance. This constructor is
        /// highly used for testing purposes to provide a specified KameleoonClient object.
        /// </summary>
        internal KameleoonProvider(string siteCode, IKameleoonClient client, IResolver resolver)
        {
            Client = client;
            _siteCode = siteCode;
            _resolver = resolver;
        }

        /// <summary>
        /// Private constructor which accepts siteCode and KameleoonClient instance. Provide default implementation
        /// of IResolver interface (Resolver class).
        /// </summary>
        private KameleoonProvider(string siteCode, IKameleoonClient client) :
                this(siteCode, client, new Resolver(client))
        { }

        /// <summary>
        /// Helper method to create a new KameleoonClient instance with error checking and conversion their types
        /// from KameleoonClient SDK to OpenFeature.
        /// </summary>
        private static IKameleoonClient MakeKameleoonClient(string siteCode, KameleoonClientConfig config)
        {
            try
            {
                return KameleoonClientFactory.Create(siteCode, config);
            }
            catch (KameleoonException.SiteCodeIsEmpty ex)
            {
                throw new FeatureProviderException(ErrorType.ProviderNotReady, innerException: ex);
            }
        }

        /// <inheritdoc/>
        public override Metadata GetMetadata()
        {
            return new Metadata(MetaName);
        }

        /// <inheritdoc/>
        public override Task<ResolutionDetails<bool>> ResolveBooleanValue(
                string flagKey, bool defaultValue, EvaluationContext? context = null) =>
            _resolver.Resolve(flagKey, defaultValue, context).AsTask();

        /// <inheritdoc/>
        public override Task<ResolutionDetails<double>> ResolveDoubleValue(
                string flagKey, double defaultValue, EvaluationContext? context = null) =>
            _resolver.Resolve(flagKey, defaultValue, context).AsTask();

        /// <inheritdoc/>
        public override Task<ResolutionDetails<int>> ResolveIntegerValue(
                string flagKey, int defaultValue, EvaluationContext? context = null) =>
            _resolver.Resolve(flagKey, defaultValue, context).AsTask();

        /// <inheritdoc/>
        public override Task<ResolutionDetails<string>> ResolveStringValue(
                string flagKey, string defaultValue, EvaluationContext? context = null) =>
            _resolver.Resolve(flagKey, defaultValue, context).AsTask();

        /// <inheritdoc/>
        public override Task<ResolutionDetails<Value>> ResolveStructureValue(
                string flagKey, Value defaultValue, EvaluationContext? context = null)
        {
            var result = _resolver.Resolve<object>(flagKey, defaultValue, context);
            return new ResolutionDetails<Value>(flagKey, DataConverter.ToOpenFeature(result.Value),
                    result.ErrorType, result.ErrorMessage).AsTask();
        }

        /// <inheritdoc/>
        public override ProviderStatus GetStatus()
        {
            var task = Client.WaitInit();
            return task.IsCompleted && !task.IsFaulted && !task.IsCanceled ?
                    ProviderStatus.Ready : ProviderStatus.NotReady;
        }

        /// <inheritdoc/>
        public override Task Initialize(EvaluationContext context) => Client.WaitInit();

        /// <inheritdoc/>
        public override Task Shutdown()
        {
            KameleoonClientFactory.Forget(_siteCode);
            return Task.CompletedTask;
        }
    }
}
