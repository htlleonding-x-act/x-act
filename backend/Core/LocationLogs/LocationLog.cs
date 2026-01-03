namespace XAct.Core.LocationLogs;

public enum TransportMode
{
    FOOT,
    BUS,
    TRAM,
    TRAIN
}

public class LocationLog
{
    public int LogId { get; init; }
    public int MemberId { get; set; }
    public Instant Timestamp { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double AccuracyMeters { get; set; }
    public TransportMode TransportMode { get; set; }
    public bool IsRevealedPosition { get; set; }
}
