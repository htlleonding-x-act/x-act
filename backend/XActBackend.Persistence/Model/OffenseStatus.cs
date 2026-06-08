namespace XActBackend.Persistence.Model;

public enum OffenseStatus
{
    /// <summary>The member is currently breaking the rule (e.g. still outside the area).</summary>
    Active = 10,

    /// <summary>The member has stopped breaking the rule (e.g. returned inside the area).</summary>
    Cleared = 20,
}
