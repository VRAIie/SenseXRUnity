namespace SenseXR.Demo.OSC.PPG
{
    public class EmotibitPI : EmotibitOscSensor
    {
        /// <summary>
        /// PPG Infrared
        /// </summary>
        public override string Name { get => "PI"; }
    }

    public class EmotibitPR : EmotibitOscSensor
    {
        /// <summary>
        /// PPG Red
        /// </summary>
        public override string Name { get => "PR"; }
    }
    public class EmotibitPG : EmotibitOscSensor
    {
        /// <summary>
        /// PPG Green
        /// </summary>
        public override string Name { get => "PG"; }
    }
}