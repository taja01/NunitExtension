namespace NUnit.Extension.DeepCompare
{
    public class Is : NUnit.Framework.Is
    {
        public static DeeplyEqualConstraint DeeplyEqualTo(object expected)
        {
            return new DeeplyEqualConstraint(expected);
        }
    }
}
