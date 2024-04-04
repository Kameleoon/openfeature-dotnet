using OpenFeature.Model;

namespace Kameleoon.OpenFeature.Tests
{

    internal struct Helpers
    {
        internal static float NextFloat(Random random)
        {
            double mantissa = (random.NextDouble() * 2.0) - 1.0;
            // choose -149 instead of -126 to also generate subnormal floats (*)
            double exponent = Math.Pow(2.0, random.Next(-126, 128));
            return (float)(mantissa * exponent);
        }
    }

    internal static class ValueExtension
    {

        internal static bool EqualWith(this Value cur, Value other)
        {
            // Compare the inner values based on their types
            if (cur.IsNull && other.IsNull)
                return true;
            if (cur.IsBoolean && other.IsBoolean)
                return cur.AsBoolean == other.AsBoolean;
            if (cur.IsNumber && other.IsNumber)
                return cur.AsDouble == other.AsDouble;
            if (cur.IsString && other.IsString)
                return cur.AsString == other.AsString;
            if (cur.IsStructure && other.IsStructure)
            {
                foreach (var kvp in cur.AsStructure)
                    if (!kvp.Value.EqualWith(other.AsStructure[kvp.Key]))
                        return false;

                return true;
            }
            if (cur.IsList && other.IsList)
            {
                if (cur.AsList.Count != other.AsList.Count)
                    return false;

                for (int i = 0; i < cur.AsList.Count; i++)
                    if (!cur.AsList[i].EqualWith(other.AsList[i]))
                        return false;

                return true;
            }
            if (cur.IsDateTime && other.IsDateTime)
                return cur.AsDateTime == other.AsDateTime;

            // If types don't match or one is null while the other is not
            return false;
        }
    }
}
