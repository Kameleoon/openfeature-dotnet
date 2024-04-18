
using System;
using System.Collections.Generic;
using System.Linq;
using Kameleoon.Data;
using Kameleoon.OpenFeature.Utils;
using Newtonsoft.Json.Linq;
using OpenFeature.Model;

namespace Kameleoon.OpenFeature
{
    /// <summary>
    /// DataConverter is used to convert a data from OpenFeature to Kameleoon.
    /// </summary>
    static class DataConverter
    {
        /// <summary>
        /// Dictionary which contains converstion methods by keys
        /// </summary>
        static readonly Dictionary<string, Func<Value, IData>> _conversionMethods =
                new Dictionary<string, Func<Value, IData>>
            {
                { Data.Type.Conversion, MakeConversion },
                { Data.Type.CustomData, MakeCustomData }
            };

        /// <summary>
        /// The method for converting EvaluationContext data to Kameleoon SDK data types.
        /// </summary>
        internal static IEnumerable<IData> ToKameleoon(EvaluationContext? context = null)
        {
            if (context == null)
                return Enumerable.Empty<IData>();

            var data = new List<IData>(context.Count);
            foreach (var kvp in context)
            {
                var values = kvp.Value.IsStructure ? kvp.Value.Yield() : kvp.Value.AsList;
                if (_conversionMethods.TryGetValue(kvp.Key, out var conversionMethod))
                    foreach (var value in values)
                        data.Add(conversionMethod(value));
            }
            return data;
        }

        /// <summary>
        /// The method for converting Kameleoon objects to OpenFeature <see cref="Value"/> instances.
        /// </summary>
        internal static Value ToOpenFeature(object? context)
        {
            switch (context)
            {
                case int intValue:
                    return new Value(intValue);
                case double doubleValue:
                    return new Value(doubleValue);
                case bool boolValue:
                    return new Value(boolValue);
                case string stringValue:
                    return new Value(stringValue);
                case JObject jObject:
                    var structureBuilder = Structure.Builder();
                    foreach (var kvp in jObject)
                        structureBuilder.Set(kvp.Key, ToOpenFeature(kvp.Value));
                    return new Value(structureBuilder.Build());
                case JArray jArray:
                    var list = new List<Value>(jArray.Count);
                    foreach (var jobj in jArray)
                        list.Add(ToOpenFeature(jobj.Value<object?>()));
                    return new Value(list);
                case JValue jValue:
                    if (jValue.Value is long value)
                        return new Value((int)value);
                    return new Value(jValue.Value);
                default:
                    return new Value();
            }
        }

        /// <summary>
        /// Make Kameleoon CustomData from <see cref="Value"/>
        /// </summary>
        private static CustomData MakeCustomData(Value value)
        {
            var structCustomData = value.AsStructure;
            var index = structCustomData.GetValue(Data.CustomDataType.Index).AsInteger ?? 0;
            structCustomData.TryGetValue(Data.CustomDataType.Values, out var structValues);
            var values = structValues?.IsString == true
                ? new[] { structValues!.AsString }
                : (structValues?.AsList ?? Enumerable.Empty<Value>())
                    .Select(v => v.AsString)
                    .Where(s => s != null)
                    .ToArray();
            return new CustomData(index, values);
        }

        /// <summary>
        /// Make Kameleoon Conversion from <see cref="Value"/>
        /// </summary>
        private static Conversion MakeConversion(Value value)
        {
            var structConversion = value.AsStructure;
            var goalId = structConversion.GetValue(Data.ConversionType.GoalId).AsInteger ?? 0;
            structConversion.TryGetValue(Data.ConversionType.Revenue, out var revenue);
            return new Conversion(goalId, (float?)revenue?.AsDouble ?? 0f);
        }
    }
}
