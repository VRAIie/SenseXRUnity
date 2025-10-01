namespace SenseXR.Demo.OSC.Heart
{
    public class EmotibitHR : EmotibitOscSensor
    {
        /// <summary>
        /// Heart Rate
        /// </summary>
        public override string Name { get => "HR"; }
    }

    public class EmotibitBI : EmotibitOscSensor
    {
        /// <summary>
        /// Heart Inter-beat Interval
        /// </summary>
        public override string Name { get => "BI"; }
    }
}