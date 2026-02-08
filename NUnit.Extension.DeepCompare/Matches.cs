namespace DeepCompare.NUnitExtension
{
    public class Matches : NUnit.Framework.Is
    {
        public static DeeplyEqualConstraint DeeplyWith(object expected, System.Action<DeepCompareOptions>? configure = null)
        {
            var options = new DeepCompareOptions();
            configure?.Invoke(options);
            return new DeeplyEqualConstraint(expected, options);
        }
    }
}
