namespace MiniBanking.SharedKernel;

public static class SystemAccountIds
{
    /// <summary>
    /// Platform clearing account used as the source/destination for top-ups,
    /// refunds, and settlement before funds reach merchant settlement.
    /// </summary>
    public static readonly Guid PlatformClearing = Guid.Parse("11111111-1111-1111-1111-111111111111");

    /// <summary>
    /// Merchant settlement account where settled funds are moved after clearing.
    /// </summary>
    public static readonly Guid MerchantSettlement = Guid.Parse("22222222-2222-2222-2222-222222222222");

    /// <summary>
    /// Platform fee account where transaction/settlement fees are credited.
    /// </summary>
    public static readonly Guid PlatformFee = Guid.Parse("33333333-3333-3333-3333-333333333333");
}
