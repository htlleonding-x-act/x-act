namespace XAct.Core.PowerUpUsages;

public enum PowerUpType
{
    BLACK_TICKET,
    DOUBLE_MOVE
}

public class PowerUpUsage
{
    public int UsageId { get; init; }
    public int MemberId { get; set; }
    public PowerUpType PowerUpType { get; set; }
    public Instant UsedAt { get; set; }
}
