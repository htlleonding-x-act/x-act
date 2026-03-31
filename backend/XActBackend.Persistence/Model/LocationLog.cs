namespace XActBackend.Persistence.Model;

public class LocationLog
{
    public int Id { get; set; }

    public int MemberId { get; set; }

    public Instant Timestamp { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public double AccuracyMeters { get; set; }

    public TransportMode TransportMode { get; set; }

    public bool IsRevealedPosition { get; set; }


    public TeamMember Member { get; set; } = null!;
}
