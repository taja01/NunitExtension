namespace DeepCompare.NUnitExtension
{
    public class Matches : NUnit.Framework.Is
    {
        public static DeeplyEqualConstraint DeeplyWith(object expected)
        {
            return new DeeplyEqualConstraint(expected);
        }
    }
}
