namespace SenseXR.Demo.OSC.EmotibitSkin
{
    public class EmotibitSA: EmotibitOscSensor
    {
        /// <summary>
        /// Skin Conductance Response (SCR) Amplitude
        /// </summary>
        public override string Name { get => "SA"; }
    }

    public class EmotibitSR : EmotibitOscSensor
    {
        /// <summary>
        /// Skin Conductance Response (SCR) Rise Time
        /// </summary>
        public override string Name { get => "SR"; }
    }

    public class EmotibitSF : EmotibitOscSensor
    {
        /// <summary>
        /// Skin Conductance Response (SCR) Frequency
        /// </summary>
        public override string Name { get => "SF"; }
    }
}