namespace SenseXR.Demo.OSC.EmotibitElectroDermal
{
    public class EmotibitEA: EmotibitOscSensor
    {
        /// <summary>
        /// EDA- Electrodermal Activity
        /// </summary>
        public override string Name { get => "EA"; }
    }

    public class EmotibitEL : EmotibitOscSensor
    {
        /// <summary>
        /// EDL- Electrodermal Level
        /// </summary>
        public override string Name { get => "EL"; }
    }

    public class EmotibitER : EmotibitOscSensor
    {
        /// <summary>
        /// EDR- Electrodermal Response (EmotiBit V4+ combines ER into EA signal)
        /// </summary>
        public override string Name { get => "ER"; }
    }
}