using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Persistence;
using MiniBanking.Modules.Accounts.Domain;
using MiniBanking.Modules.Admin.Domain;
using MiniBanking.Modules.Audit.Application.Queries.GetAuditLogs;
using MiniBanking.Modules.Payments.Application.Commands.AdminRefund;
using MiniBanking.Modules.Payments.Application.CreateRefund;
using MiniBanking.Modules.Payments.Application.Queries.GetPaymentById;
using MiniBanking.Modules.Payments.Application.Queries.GetPayments;
using MiniBanking.Modules.Payments.Domain;
using MiniBanking.SharedKernel;
using Xunit;

namespace MiniBanking.Tests;

public class AuditAndPaymentCqrsTests
{
    private MiniBankingDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<MiniBankingDbContext>()
            .UseInMemoryDatabase(databaseName: $"AuditAndPaymentTestDb_{Guid.NewGuid():N}")
            .Options;

        return new MiniBankingDbContext(options);
    }

    [Fact]
    public async Task GetAuditLogsHandler_ShouldFilterAndPaginateCorrectly()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();

        db.AuditLogs.AddRange(
            new AuditLog("admin1", "admin1@test.com", "CreatePayment", "/api/v1/merchant/payments", "POST", "/api/v1/merchant/payments", null, 200, "127.0.0.1", "corr-1"),
            new AuditLog("admin2", "admin2@test.com", "TopUpWallet", "/api/v1/admin/wallets/top-up", "POST", "/api/v1/admin/wallets/top-up", null, 200, "127.0.0.1", "corr-2"),
            new AuditLog("admin1", "admin1@test.com", "RefundPayment", "/api/v1/admin/payments/refund", "POST", "/api/v1/admin/payments/refund", null, 200, "127.0.0.1", "corr-3")
        );
        await db.SaveChangesAsync();

        var handler = new GetAuditLogsHandler(db);

        // Act - filter by actor "admin1"
        var query = new GetAuditLogsQuery(Page: 1, PageSize: 10, Action: null, Actor: "admin1", FromDate: null, ToDate: null);
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, item => Assert.Equal("admin1@test.com", item.ActorEmail));

        // Act - filter by action "TopUp"
        var queryAction = new GetAuditLogsQuery(Page: 1, PageSize: 10, Action: "TopUp", Actor: null, FromDate: null, ToDate: null);
        var resultAction = await handler.Handle(queryAction, CancellationToken.None);

        // Assert
        Assert.Equal(1, resultAction.TotalCount);
        Assert.Equal("TopUpWallet", resultAction.Items[0].Action);
    }

    [Fact]
    public async Task GetPaymentsHandler_ShouldJoinWalletsAndCustomers_WithFilters()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();

        var customer = new BankingCustomer("Nguyen Van C", "c@test.com", "0900000003");
        var wallet = new WalletAccount(customer, "WA-PAY-001", "VND");
        db.BankingCustomers.Add(customer);
        db.WalletAccounts.Add(wallet);

        var payment1 = new Payment("MCH-001", "ORD-1", wallet.Id, Money.Vnd(100_000), "Payment 1", null, "key-1");
        payment1.MarkSucceeded(Guid.NewGuid());

        var payment2 = new Payment("MCH-002", "ORD-2", wallet.Id, Money.Vnd(200_000), "Payment 2", null, "key-2");
        payment2.MarkFailed("INSUFFICIENT_FUNDS");

        db.Payments.AddRange(payment1, payment2);
        await db.SaveChangesAsync();

        var handler = new GetPaymentsHandler(db);

        // Act - get all payments
        var query = new GetPaymentsQuery(Page: 1, PageSize: 20);
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.TotalCount);
        var item1 = result.Items.First(x => x.PaymentId == payment1.Id);
        Assert.Equal("WA-PAY-001", item1.WalletAccountNumber);
        Assert.Equal("Nguyen Van C", item1.CustomerName);
        Assert.Equal("c@test.com", item1.CustomerEmail);
        Assert.Equal("Succeeded", item1.Status);

        // Act - filter by status Succeeded
        var queryStatus = new GetPaymentsQuery(Status: "Succeeded");
        var resultStatus = await handler.Handle(queryStatus, CancellationToken.None);

        // Assert
        Assert.Single(resultStatus.Items);
        Assert.Equal(payment1.Id, resultStatus.Items[0].PaymentId);

        // Act - filter by merchantId
        var queryMerchant = new GetPaymentsQuery(MerchantId: "MCH-002");
        var resultMerchant = await handler.Handle(queryMerchant, CancellationToken.None);

        // Assert
        Assert.Single(resultMerchant.Items);
        Assert.Equal("MCH-002", resultMerchant.Items[0].MerchantId);
        Assert.Equal("Failed", resultMerchant.Items[0].Status);
    }

    [Fact]
    public async Task GetPaymentByIdHandler_ShouldReturnPaymentDetailWithRefunds()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();

        var customer = new BankingCustomer("Tran Thi D", "d@test.com", "0900000004");
        var wallet = new WalletAccount(customer, "WA-PAY-002", "VND");
        db.BankingCustomers.Add(customer);
        db.WalletAccounts.Add(wallet);

        var payment = new Payment("MCH-001", "ORD-100", wallet.Id, Money.Vnd(500_000), "Detail test", null, "key-detail");
        payment.MarkSucceeded(Guid.NewGuid());
        db.Payments.Add(payment);

        var refund = new Refund("MCH-001", "REF-001", payment.Id, Money.Vnd(100_000), "Customer requested", "idemp-ref-1");
        refund.MarkSucceeded(Guid.NewGuid());
        db.Refunds.Add(refund);

        await db.SaveChangesAsync();

        var handler = new GetPaymentByIdHandler(db);

        // Act
        var result = await handler.Handle(new GetPaymentByIdQuery(payment.Id), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(payment.Id, result.PaymentId);
        Assert.Equal("WA-PAY-002", result.WalletAccountNumber);
        Assert.Equal("Tran Thi D", result.CustomerName);
        Assert.Equal("d@test.com", result.CustomerEmail);
        Assert.Single(result.Refunds);
        Assert.Equal("REF-001", result.Refunds[0].MerchantRefundId);
        Assert.Equal(100_000, result.Refunds[0].Amount);

        // Act - non existent payment
        var notFoundResult = await handler.Handle(new GetPaymentByIdQuery(Guid.NewGuid()), CancellationToken.None);
        Assert.Null(notFoundResult);
    }

    [Fact]
    public async Task AdminRefundHandler_ShouldDelegateToCreateRefundCommand()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();

        var customer = new BankingCustomer("Le Van E", "e@test.com", "0900000005");
        var wallet = new WalletAccount(customer, "WA-PAY-003", "VND");
        db.BankingCustomers.Add(customer);
        db.WalletAccounts.Add(wallet);

        var payment = new Payment("MCH-001", "ORD-200", wallet.Id, Money.Vnd(300_000), "Refund delegate test", null, "key-ref-del");
        payment.MarkSucceeded(Guid.NewGuid());
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var fakeMediator = new FakeMediator();
        var expectedResponse = new CreateRefundResponse(
            Guid.NewGuid(),
            "ADM-REF-123",
            payment.Id,
            "Succeeded",
            300_000,
            "VND",
            null);

        fakeMediator.Handler = req =>
        {
            if (req is CreateRefundCommand c)
            {
                Assert.Equal("MCH-001", c.MerchantId);
                Assert.Equal(payment.Id, c.Request.PaymentId);
                Assert.Equal(300_000, c.Request.Amount);
                return expectedResponse;
            }
            throw new InvalidOperationException("Unexpected command");
        };

        var handler = new AdminRefundHandler(db, fakeMediator);

        // Act
        var result = await handler.Handle(new AdminRefundCommand(payment.Id, null, "Admin test refund"), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Succeeded", result.Status);
        Assert.Equal(payment.Id, result.PaymentId);
        Assert.Equal(1, fakeMediator.SendCallCount);
    }

    [Fact]
    public async Task AdminRefundHandler_ShouldThrowKeyNotFoundException_WhenPaymentDoesNotExist()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var fakeMediator = new FakeMediator();
        var handler = new AdminRefundHandler(db, fakeMediator);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            handler.Handle(new AdminRefundCommand(Guid.NewGuid(), 100_000, "Reason"), CancellationToken.None));
    }

    private class FakeMediator : IMediator
    {
        public Func<object, object?>? Handler { get; set; }
        public int SendCallCount { get; private set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            SendCallCount++;
            var res = Handler != null ? (TResponse)Handler(request)! : default!;
            return Task.FromResult(res);
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            SendCallCount++;
            var res = Handler != null ? Handler(request) : null;
            return Task.FromResult(res);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
        {
            SendCallCount++;
            Handler?.Invoke(request);
            return Task.CompletedTask;
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
            => Task.CompletedTask;

        public Task Publish(object notification, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }
}
