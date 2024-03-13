namespace NUnit.Extension.DeepCompare
{
    public class Matches : Framework.Is
    {
        public static DeeplyEqualConstraint DeeplyWith(object expected)
        {
            return new DeeplyEqualConstraint(expected);
        }
    }
}
