namespace MiniBanking.SharedKernel;

public static class SystemAccountIds
{
    /// <summary>
    /// Platform clearing account used as the source/destination for top-ups,
    /// refunds, and settlement before funds reach merchant settlement.
    /// </summary>
    public static readonly Guid PlatformClearing = Guid.Parse("11111111-1111-1111-1111-111111111111");
}
