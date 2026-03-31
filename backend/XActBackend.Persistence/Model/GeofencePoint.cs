namespace XActBackend.Persistence.Model;

public class GeofencePoint
{
    public int Id { get; set; }

    public int SessionId { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public int SequenceOrder { get; set; }


    public GameSession Session { get; set; } = null!;
}
