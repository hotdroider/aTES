using aTES.Common;
using aTES.Common.Kafka;
using aTES.PaymentProcessor.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;

namespace aTES.PaymentProcessor
{
    public class PaymentService : BackgroundService
    {
        private readonly ILogger<PaymentService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        private readonly MailConfig _mailConfig;

        private readonly IConsumer _paymentConsumer;

        public PaymentService(ILogger<PaymentService> logger,
            IOptions<MailConfig> options,
            IServiceScopeFactory scopeFactory,
            IConsumerFactory consumerFactory)
        {
            _mailConfig = options.Value;
            _scopeFactory = scopeFactory;
            _logger = logger;
            _paymentConsumer = consumerFactory
                .DefineConsumer<TransactionData>("aTES.PaymentService", "BillingTransactions")
                .SetFailoverPolicy(FailoverPolicy.ToDlq)
                .SetProcessor(ProcessPaymentMessage)
                .Build();
        }

        private async Task ProcessPaymentMessage(Event<TransactionData> transactionMsg)
        {
            if (transactionMsg.Name != "Accounts.Paid")
                return;

            switch (transactionMsg.Name, transactionMsg.Version)
            {
                case ("Accounts.Paid", 1):
                    await ProcessPaymentAsync(transactionMsg);
                    return;
                default:
                    throw new Exception($"Unsupported message {transactionMsg.Name}{transactionMsg.Version}");

            }
        }

        private async Task ProcessPaymentAsync(Event<TransactionData> payMessage)
        {
            _logger.LogInformation($"Processing payment {payMessage.Data.Amount} for account {payMessage.Data.AccountPublicKey}");

            await SendEmail(payMessage);

            //here we go to the bank! 
        }

        private async Task SendEmail(Event<TransactionData> payMessage)
        {
            var db = _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<PaymentDbContext>();

            var email = await db.Accounts
                .Where(a => a.PublicKey == payMessage.Data.AccountPublicKey)
                .Select(a => a.Email)
                .FirstOrDefaultAsync();

            var message = new MailMessage(_mailConfig.From, email);
            message.Subject = $"Payment for {payMessage.Data.Amount}";
            message.Body = @"Using this new feature, you can send an email message from an application very easily.";
            SmtpClient client = new SmtpClient(_mailConfig.Server);

            if (_mailConfig.UseCredentials)
                client.Credentials = new NetworkCredential(_mailConfig.User, _mailConfig.Password);
            else
                client.UseDefaultCredentials = true;

            try
            {
                client.Send(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot send email");
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) => await _paymentConsumer.ProcessAsync(stoppingToken);

        private class TransactionData
        {
            public string AccountPublicKey { get; set; }

            public DateTime Date { get; set; }

            public decimal Amount { get; set; }

            public string Reason { get; set; }
        }
    }
}
