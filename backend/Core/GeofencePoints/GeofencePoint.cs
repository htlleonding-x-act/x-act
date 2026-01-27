namespace XAct.Core.GeofencePoints;

public class GeofencePoint
{
    public int PointId { get; init; }
    public int SessionId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int SequenceOrder { get; set; }
}
