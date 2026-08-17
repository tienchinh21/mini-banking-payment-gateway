namespace MiniBanking.Modules.Ledger.Domain;

public enum LedgerTransactionType
{
    TopUp = 1,
    Payment = 2,
    Refund = 3,
    Settlement = 4,
    Adjustment = 5
}
