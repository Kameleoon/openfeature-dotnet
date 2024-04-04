
using System.Linq;
using System.Runtime.CompilerServices;
using Kameleoon.OpenFeature.Utils;
using OpenFeature.Constant;
using OpenFeature.Model;

[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace Kameleoon.OpenFeature
{
    /// <summary>
    /// IResolver interface which contains method for evalutions based on provided data
    /// </summary>
    interface IResolver
    {
        ResolutionDetails<T> Resolve<T>(string flagKey, T defaultValue, EvaluationContext? context = null);
    }

    /// <summary>
    /// Resolver makes evalutions based on provided data, conforms to IResolver interface
    /// </summary>
    sealed class Resolver : IResolver
    {
        private readonly IKameleoonClient _client;

        internal Resolver(IKameleoonClient client)
        {
            _client = client;
        }

        /// <summary>
        /// Main method for getting resolition details based on provided data.
        /// </summary>
        public ResolutionDetails<T> Resolve<T>(string flagKey, T defaultValue, EvaluationContext? context = null)
        {
            try
            {
                // Get visitor code
                var visitorCode = context?.TargetingKey;
                if (string.IsNullOrEmpty(visitorCode))
                    return MakeResolutionDetails(flagKey, defaultValue, ErrorType.TargetingKeyMissing,
                        "The TargetingKey is required in context and cannot be ommited.");

                // Add targeting data from context to KameleoonClient by visitor code
                _client.AddData(visitorCode, DataConverter.ToKameleoon(context).ToArray());
                // Get a variant
                var variant = _client.GetFeatureVariationKey(visitorCode, flagKey);
                // Get the all variables for the variant
                var variables = _client.GetFeatureVariationVariables(flagKey, variant);
                // Get variableKey if it's provided in context or any first in variation. It's responsibility of
                // the client to have only one variable per variation if variableKey is not provided.
                string variableKey = Variable.TryGet(context) ?? variables.Keys.FirstOrDefault();
                // Try to get value by variable key
                if (variableKey == null || !variables.TryGetValue(variableKey, out var value))
                    return MakeResolutionDetails(flagKey, defaultValue, ErrorType.FlagNotFound,
                        Variable.MakeErrorDescription(variant, variableKey), variant);

                // Check if the variable value has a required type
                if (!(value is T typedValue))
                    return MakeResolutionDetails(flagKey, defaultValue, ErrorType.TypeMismatch,
                        "The type of value received is different from the requested value.", variant);

                return MakeResolutionDetails(flagKey, typedValue, variant: variant);
            }
            catch (KameleoonException exception)
            {
                var (errorType, message) = exception.ToOpenFeatureError();
                return new ResolutionDetails<T>(flagKey, defaultValue, errorType, errorMessage: message);
            }
        }

        /// <summary>
        /// Helper method to make <see cref="ResolutionDetails<T>"/> object.
        /// </summary>
        private static ResolutionDetails<T> MakeResolutionDetails<T>(
                string flagKey, T defaultValue, ErrorType errorType = ErrorType.None, string? errorMessage = null,
                string? variant = null) =>
            new ResolutionDetails<T>(flagKey, defaultValue, errorType, errorMessage: errorMessage, variant: variant);
    }
}
