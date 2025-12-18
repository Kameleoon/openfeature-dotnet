using System.Collections.Generic;
using System.Threading.Tasks;
using OpenFeature.Constant;
using OpenFeature.Model;

namespace Kameleoon.OpenFeature.Utils
{
    internal struct Variable
    {
        internal const string VariableKey = "variableKey";

        internal static string? TryGet(EvaluationContext? context = null) =>
            context?.TryGetValue(VariableKey, out var value) == true ? value.AsString : null;

        internal static string MakeErrorDescription(string variant, string? variableKey) =>
            string.IsNullOrEmpty(variableKey) ? $"The variation '{variant}' has no variables" :
                $"The value for provided variable key '{variableKey}' isn't found in variation '{variant}'";
    }


    internal static class KameleoonExceptionExt
    {
        internal static (ErrorType type, string? message) ToOpenFeatureError(this KameleoonException exception)
        {
            if (exception is KameleoonException.FeatureException)
                return (ErrorType.FlagNotFound, exception.Message);
            if (exception is KameleoonException.VisitorCodeInvalid)
                return (ErrorType.InvalidContext, exception.Message);
            return (ErrorType.General, exception.Message);
        }
    }

    internal static class ResolutionDetailsExt
    {
        internal static Task<ResolutionDetails<T>> AsTask<T>(this ResolutionDetails<T> details) =>
                    Task.FromResult(details);
    }

    internal static class IEnumerableExt
    {
        /// <summary>
        /// Wraps this object instance into an IEnumerable&lt;T&gt; which contains only single element.
        /// </summary>
        /// <typeparam name="T"> Type of the object. </typeparam>
        /// <param name="item"> The instance that will be wrapped. </param>
        /// <returns> An IEnumerable&lt;T&gt; consisting of a single item. </returns>
        public static IEnumerable<T> Yield<T>(this T item)
        {
            yield return item;
        }
    }
}
