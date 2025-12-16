namespace WassControlSys.Models
{
    public class DiskHealthInfo
    {
        public string DeviceId { get; set; }
        public string Model { get; set; }
        public string Serial { get; set; }
        public bool SmartOk { get; set; }
        public bool SmartStatusKnown { get; set; }
    }
}

