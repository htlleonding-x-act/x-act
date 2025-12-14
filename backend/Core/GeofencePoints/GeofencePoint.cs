namespace XAct.Core.GeofencePoints;

public class GeofencePoint
{
    public Guid PointId { get; init; }
    public Guid SessionId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int SequenceOrder { get; set; }
}
