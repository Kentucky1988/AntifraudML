using FraudDetection.Core.Models;

namespace FraudDetection.Demo;

/// <summary>
/// Generates synthetic transaction data for demonstration and testing.
/// Creates realistic counter values with fraud patterns.
/// </summary>
public static class SampleDataGenerator
{
    private static readonly Random _random = new(42);

    /// <summary>
    /// Generates a dataset of synthetic transactions with fraud labels.
    /// </summary>
    /// <param name="count">Number of transactions to generate.</param>
    /// <param name="fraudRate">Percentage of fraudulent transactions (0.0 to 1.0).</param>
    /// <returns>Training data with transactions and labels.</returns>
    public static TrainingData GenerateTrainingData(int count, float fraudRate = 0.1f)
    {
        var transactions = new Transaction[count];
        var labels = new bool[count];
        int fraudCount = (int)(count * fraudRate);

        for (int i = 0; i < count; i++)
        {
            bool isFraud = i < fraudCount;
            transactions[i] = GenerateTransaction($"TX-{i:D6}", isFraud);
            labels[i] = isFraud;
        }

        // Shuffle to mix fraud and legitimate transactions
        ShuffleData(transactions, labels);

        return new TrainingData
        {
            Transactions = transactions,
            FraudLabels = labels
        };
    }

    /// <summary>
    /// Generates a single transaction with realistic counter values.
    /// </summary>
    /// <param name="transactionId">Transaction identifier.</param>
    /// <param name="isFraud">Whether to generate fraud patterns.</param>
    /// <returns>Generated transaction.</returns>
    public static Transaction GenerateTransaction(string transactionId, bool isFraud)
    {
        var counters = new TransactionCounters();

        if (isFraud)
        {
            GenerateFraudCounters(counters);
        }
        else
        {
            GenerateLegitimateCounters(counters);
        }

        return new Transaction
        {
            TransactionId = transactionId,
            Timestamp = DateTime.UtcNow.AddMinutes(-_random.Next(0, 10080)), // Last 7 days
            Amount = isFraud ? GenerateFraudAmount() : GenerateLegitimateAmount(),
            UserId = $"USER-{_random.Next(1000, 9999)}",
            Counters = counters
        };
    }

    private static void GenerateLegitimateCounters(TransactionCounters counters)
    {
        // Phone counters - low values for legitimate users
        counters.SetCounter(DepositCounter.CountIpCountriesPhone1d, _random.Next(1, 3));
        counters.SetCounter(DepositCounter.CountIpCountriesPhone7d, _random.Next(1, 4));
        counters.SetCounter(DepositCounter.CountIpCountriesPhone30d, _random.Next(1, 5));
        counters.SetCounter(DepositCounter.CountDevicesPhone1d, _random.Next(1, 2));
        counters.SetCounter(DepositCounter.CountDevicesPhone7d, _random.Next(1, 3));
        counters.SetCounter(DepositCounter.CountDevicesPhone30d, _random.Next(1, 4));
        counters.SetCounter(DepositCounter.CountCardsPhone1d, _random.Next(1, 2));
        counters.SetCounter(DepositCounter.CountCardsPhone7d, _random.Next(1, 3));
        counters.SetCounter(DepositCounter.CountCardsPhone30d, _random.Next(1, 4));

        // User counters - normal patterns
        counters.SetCounter(DepositCounter.CountFailsTransUser1h, _random.Next(0, 2));
        counters.SetCounter(DepositCounter.CountFailsTransUser1hInRow, _random.Next(0, 1));
        counters.SetCounter(DepositCounter.CountFailsTransUser3dInRow, _random.Next(0, 2));
        counters.SetCounter(DepositCounter.CountCardsUser1d, _random.Next(1, 2));
        counters.SetCounter(DepositCounter.CountCardsUser7d, _random.Next(1, 3));
        counters.SetCounter(DepositCounter.CountCardsUser14d, _random.Next(1, 3));
        counters.SetCounter(DepositCounter.CountCardsUser30d, _random.Next(1, 4));

        // Device fingerprints - stable for legitimate users
        counters.SetCounter(DepositCounter.CountDeviceFingerprintsUser1d, _random.Next(1, 2));
        counters.SetCounter(DepositCounter.CountDeviceFingerprintsUser7d, _random.Next(1, 3));
        counters.SetCounter(DepositCounter.CountDeviceFingerprintsUser30d, _random.Next(1, 4));

        // Phones - stable
        counters.SetCounter(DepositCounter.CountPhonesUser1d, 1);
        counters.SetCounter(DepositCounter.CountPhonesUser7d, _random.Next(1, 2));
        counters.SetCounter(DepositCounter.CountPhonesUser14d, _random.Next(1, 2));

        // Risk declines - low for legitimate
        counters.SetCounter(DepositCounter.CountLowRiskDeclinesUser1d, _random.Next(0, 1));
        counters.SetCounter(DepositCounter.CountMediumRiskDeclinesUser1d, 0);
        counters.SetCounter(DepositCounter.CountHighRiskDeclinesUser1d, 0);

        // IP counters - stable
        counters.SetCounter(DepositCounter.CountIpUser1d, _random.Next(1, 3));
        counters.SetCounter(DepositCounter.CountIpUser7d, _random.Next(1, 5));
        counters.SetCounter(DepositCounter.CountIpUser14d, _random.Next(1, 7));
        counters.SetCounter(DepositCounter.CountIpCountriesUser1d, 1);
        counters.SetCounter(DepositCounter.CountIpCountriesUser7d, _random.Next(1, 2));
        counters.SetCounter(DepositCounter.CountIpCountriesUser14d, _random.Next(1, 2));

        // Email counters - stable
        counters.SetCounter(DepositCounter.CountEmailsUser1d, 1);
        counters.SetCounter(DepositCounter.CountEmailsUser7d, 1);
        counters.SetCounter(DepositCounter.CountEmailsUser14d, 1);
        counters.SetCounter(DepositCounter.CountEmailsUser30d, _random.Next(1, 2));
        counters.SetCounter(DepositCounter.CountWalletsUser30d, _random.Next(1, 3));

        // Time between transactions - normal intervals
        counters.SetCounter(DepositCounter.AverageSecondsBetweenTransactionsUser10m, _random.Next(60, 600));
        counters.SetCounter(DepositCounter.AverageSecondsBetweenTransactionsUser30m, _random.Next(120, 1800));
        counters.SetCounter(DepositCounter.AverageSecondsBetweenTransactionsUser1h, _random.Next(300, 3600));
        counters.SetCounter(DepositCounter.AverageSecondsBetweenTransactionsUser1d, _random.Next(3600, 86400));

        // Average amounts - consistent
        float avgAmount = _random.Next(50, 500);
        counters.SetCounter(DepositCounter.AverageAmountAllDepositsUserEur1d, avgAmount);
        counters.SetCounter(DepositCounter.AverageAmountAllDepositsUserEur7d, avgAmount * (1 + (float)(_random.NextDouble() * 0.2 - 0.1)));
        counters.SetCounter(DepositCounter.AverageAmountAllDepositsUserEur14d, avgAmount * (1 + (float)(_random.NextDouble() * 0.3 - 0.15)));
        counters.SetCounter(DepositCounter.AverageAmountAllDepositsUserEur30d, avgAmount * (1 + (float)(_random.NextDouble() * 0.4 - 0.2)));
        counters.SetCounter(DepositCounter.AverageAmountAllDepositsUserEur60d, avgAmount * (1 + (float)(_random.NextDouble() * 0.5 - 0.25)));

        // Success amounts
        counters.SetCounter(DepositCounter.AverageAmountAllSuccessDepositsEurUser1d, avgAmount);
        counters.SetCounter(DepositCounter.AverageAmountAllSuccessDepositsEurUser7d, avgAmount);
        counters.SetCounter(DepositCounter.AverageAmountAllSuccessDepositsEurUser14d, avgAmount);
        counters.SetCounter(DepositCounter.AverageAmountAllSuccessDepositsEurUser30d, avgAmount);
        counters.SetCounter(DepositCounter.AverageAmountAllSuccessDepositsEurUser60d, avgAmount);

        // Transaction counts - moderate
        counters.SetCounter(DepositCounter.CountTransactionsUser1h, _random.Next(1, 5));
        counters.SetCounter(DepositCounter.CountPendingTransactionsUser1h, _random.Next(0, 2));
        counters.SetCounter(DepositCounter.CountPendingTransactionsUser1d, _random.Next(0, 3));
        counters.SetCounter(DepositCounter.CountSuccessPayments30dInRow, _random.Next(5, 30));

        // Same amount transactions - low
        counters.SetCounter(DepositCounter.CountTransactionsWithSameAmountInRowUser1d, _random.Next(0, 2));
        counters.SetCounter(DepositCounter.CountTransactionsWithSameAmountInRowUser7d, _random.Next(0, 3));
        counters.SetCounter(DepositCounter.CountTransactionsWithSameAmountInRowUser14d, _random.Next(0, 4));
        counters.SetCounter(DepositCounter.CountTransactionsWithSameAmountInRowUser30d, _random.Next(0, 5));

        // Success payments - good history
        counters.SetCounter(DepositCounter.CountSuccessPaymentsUser1d, _random.Next(1, 5));
        counters.SetCounter(DepositCounter.CountSuccessPaymentsUser7d, _random.Next(5, 20));
        counters.SetCounter(DepositCounter.CountSuccessPaymentsUser14d, _random.Next(10, 40));
        counters.SetCounter(DepositCounter.CountSuccessPaymentsUser30d, _random.Next(20, 60));
        counters.SetCounter(DepositCounter.CountSuccessPayments60d, _random.Next(40, 120));

        // Success to fail ratio - high for legitimate
        counters.SetCounter(DepositCounter.CountSuccessToFailTransactionsRatioUser1h, _random.Next(80, 100) / 100f);
        counters.SetCounter(DepositCounter.CountSuccessToFailTransactionsRatioUser1d, _random.Next(85, 100) / 100f);
        counters.SetCounter(DepositCounter.CountSuccessToFailTransactionsRatioUser7d, _random.Next(90, 100) / 100f);
        counters.SetCounter(DepositCounter.CountSuccessToFailTransactionsRatioUser14d, _random.Next(90, 100) / 100f);
        counters.SetCounter(DepositCounter.CountSuccessToFailTransactionsRatioUser30d, _random.Next(90, 100) / 100f);

        // Deposit to payout ratio - balanced
        counters.SetCounter(DepositCounter.PercentAmountSuccessDepositToPayoutUser1d, _random.Next(40, 80) / 100f);
        counters.SetCounter(DepositCounter.PercentAmountSuccessDepositToPayoutUser7d, _random.Next(40, 80) / 100f);
        counters.SetCounter(DepositCounter.PercentAmountSuccessDepositToPayoutUser14d, _random.Next(40, 80) / 100f);
        counters.SetCounter(DepositCounter.PercentAmountSuccessDepositToPayoutUser30d, _random.Next(40, 80) / 100f);
        counters.SetCounter(DepositCounter.PercentAmountSuccessDepositToPayoutUser60d, _random.Next(40, 80) / 100f);

        // Payment methods - stable
        counters.SetCounter(DepositCounter.CountUniquePaymentMethodsUser1h, 1);
        counters.SetCounter(DepositCounter.CountUniquePaymentMethodsUser1d, _random.Next(1, 2));
        counters.SetCounter(DepositCounter.CountUniquePaymentMethodsUser7d, _random.Next(1, 3));
        counters.SetCounter(DepositCounter.CountUniquePaymentMethodsUser14d, _random.Next(1, 3));
        counters.SetCounter(DepositCounter.CountUniquePaymentMethodsUser30d, _random.Next(1, 4));

        // Time since last deposit - normal
        counters.SetCounter(DepositCounter.CountTimeSinceLastDepositInSecondsUser, _random.Next(3600, 86400));
        counters.SetCounter(DepositCounter.CountTimeSinceLastFailedDepositInSecondsUser, _random.Next(86400, 604800));
        counters.SetCounter(DepositCounter.CountTimeSinceLastSuccessDepositInSecondsUser, _random.Next(3600, 86400));
        counters.SetCounter(DepositCounter.CountAmountDeviationRatioFromUserAvgUser, (float)(_random.NextDouble() * 0.3));

        // Card sums - moderate
        counters.SetCounter(DepositCounter.SumSuccessDepositsCard1h, _random.Next(0, 500));
        counters.SetCounter(DepositCounter.SumSuccessDepositsCard1d, _random.Next(100, 1000));
        counters.SetCounter(DepositCounter.SumSuccessDepositsCard7d, _random.Next(500, 3000));
        counters.SetCounter(DepositCounter.SumSuccessDepositsCard14d, _random.Next(1000, 5000));
        counters.SetCounter(DepositCounter.SumSuccessDepositsCard30d, _random.Next(2000, 10000));
        counters.SetCounter(DepositCounter.SumSuccessDepositsCard60d, _random.Next(4000, 20000));

        // Country mismatch - rare for legitimate
        counters.SetCounter(DepositCounter.IsTransactionCountryMismatch, _random.NextDouble() < 0.05 ? 1 : 0);
        counters.SetCounter(DepositCounter.HourOfDay, _random.Next(8, 22)); // Normal hours
        counters.SetCounter(DepositCounter.DayOfWeek, _random.Next(0, 7));

        // User history - established
        counters.SetCounter(DepositCounter.CountUserCountCountryMismatchTransactionsUser, _random.Next(0, 2));
        counters.SetCounter(DepositCounter.CountUserTimeFromRegistrationToFirstDepInSeconds, _random.Next(86400, 604800));
        counters.SetCounter(DepositCounter.CountUserTimeFromFirstSuccessDepInSeconds30d, _random.Next(2592000, 7776000));
        counters.SetCounter(DepositCounter.CountUserTimeFromRegistrationInSeconds, _random.Next(2592000, 31536000));

        // Antifraud declines - low
        counters.SetCounter(DepositCounter.CountAntifraudDeclinesUser1h, 0);
        counters.SetCounter(DepositCounter.CountAntifraudDeclinesUser1d, _random.Next(0, 1));
        counters.SetCounter(DepositCounter.CountAntifraudDeclinesUser7d, _random.Next(0, 2));
        counters.SetCounter(DepositCounter.CountAntifraudDeclinesUser14d, _random.Next(0, 3));
        counters.SetCounter(DepositCounter.CountAntifraudDeclinesUser30d, _random.Next(0, 4));

        // Document counters
        counters.SetCounter(DepositCounter.CountCardsDocument1d, _random.Next(1, 2));
        counters.SetCounter(DepositCounter.CountCardsDocument7d, _random.Next(1, 3));
        counters.SetCounter(DepositCounter.CountCardsDocument30d, _random.Next(1, 4));

        // IP session counters - low sharing
        SetLegitimateIpCounters(counters);
        SetLegitimateEmailCounters(counters);
        SetLegitimateCardCounters(counters);
        SetLegitimateUserIpCounters(counters);
    }


    private static void GenerateFraudCounters(TransactionCounters counters)
    {
        // Phone counters - high values indicating multiple identities
        counters.SetCounter(DepositCounter.CountIpCountriesPhone1d, _random.Next(3, 10));
        counters.SetCounter(DepositCounter.CountIpCountriesPhone7d, _random.Next(5, 15));
        counters.SetCounter(DepositCounter.CountIpCountriesPhone30d, _random.Next(8, 20));
        counters.SetCounter(DepositCounter.CountDevicesPhone1d, _random.Next(3, 8));
        counters.SetCounter(DepositCounter.CountDevicesPhone7d, _random.Next(5, 12));
        counters.SetCounter(DepositCounter.CountDevicesPhone30d, _random.Next(8, 20));
        counters.SetCounter(DepositCounter.CountCardsPhone1d, _random.Next(3, 8));
        counters.SetCounter(DepositCounter.CountCardsPhone7d, _random.Next(5, 15));
        counters.SetCounter(DepositCounter.CountCardsPhone30d, _random.Next(10, 25));

        // User counters - suspicious patterns
        counters.SetCounter(DepositCounter.CountFailsTransUser1h, _random.Next(3, 10));
        counters.SetCounter(DepositCounter.CountFailsTransUser1hInRow, _random.Next(2, 6));
        counters.SetCounter(DepositCounter.CountFailsTransUser3dInRow, _random.Next(3, 10));
        counters.SetCounter(DepositCounter.CountCardsUser1d, _random.Next(3, 8));
        counters.SetCounter(DepositCounter.CountCardsUser7d, _random.Next(5, 15));
        counters.SetCounter(DepositCounter.CountCardsUser14d, _random.Next(8, 20));
        counters.SetCounter(DepositCounter.CountCardsUser30d, _random.Next(10, 30));

        // Device fingerprints - many devices
        counters.SetCounter(DepositCounter.CountDeviceFingerprintsUser1d, _random.Next(3, 8));
        counters.SetCounter(DepositCounter.CountDeviceFingerprintsUser7d, _random.Next(5, 15));
        counters.SetCounter(DepositCounter.CountDeviceFingerprintsUser30d, _random.Next(10, 25));

        // Phones - multiple
        counters.SetCounter(DepositCounter.CountPhonesUser1d, _random.Next(2, 5));
        counters.SetCounter(DepositCounter.CountPhonesUser7d, _random.Next(3, 8));
        counters.SetCounter(DepositCounter.CountPhonesUser14d, _random.Next(5, 12));

        // Risk declines - high
        counters.SetCounter(DepositCounter.CountLowRiskDeclinesUser1d, _random.Next(2, 5));
        counters.SetCounter(DepositCounter.CountMediumRiskDeclinesUser1d, _random.Next(1, 4));
        counters.SetCounter(DepositCounter.CountHighRiskDeclinesUser1d, _random.Next(1, 3));

        // IP counters - many IPs
        counters.SetCounter(DepositCounter.CountIpUser1d, _random.Next(5, 15));
        counters.SetCounter(DepositCounter.CountIpUser7d, _random.Next(10, 30));
        counters.SetCounter(DepositCounter.CountIpUser14d, _random.Next(15, 50));
        counters.SetCounter(DepositCounter.CountIpCountriesUser1d, _random.Next(2, 5));
        counters.SetCounter(DepositCounter.CountIpCountriesUser7d, _random.Next(3, 8));
        counters.SetCounter(DepositCounter.CountIpCountriesUser14d, _random.Next(5, 12));

        // Email counters - multiple emails
        counters.SetCounter(DepositCounter.CountEmailsUser1d, _random.Next(2, 5));
        counters.SetCounter(DepositCounter.CountEmailsUser7d, _random.Next(3, 8));
        counters.SetCounter(DepositCounter.CountEmailsUser14d, _random.Next(5, 12));
        counters.SetCounter(DepositCounter.CountEmailsUser30d, _random.Next(8, 20));
        counters.SetCounter(DepositCounter.CountWalletsUser30d, _random.Next(5, 15));

        // Time between transactions - very fast (bot-like)
        counters.SetCounter(DepositCounter.AverageSecondsBetweenTransactionsUser10m, _random.Next(5, 30));
        counters.SetCounter(DepositCounter.AverageSecondsBetweenTransactionsUser30m, _random.Next(10, 60));
        counters.SetCounter(DepositCounter.AverageSecondsBetweenTransactionsUser1h, _random.Next(20, 120));
        counters.SetCounter(DepositCounter.AverageSecondsBetweenTransactionsUser1d, _random.Next(60, 600));

        // Average amounts - erratic
        float avgAmount = _random.Next(500, 5000);
        counters.SetCounter(DepositCounter.AverageAmountAllDepositsUserEur1d, avgAmount);
        counters.SetCounter(DepositCounter.AverageAmountAllDepositsUserEur7d, avgAmount * (1 + (float)(_random.NextDouble() * 2 - 1)));
        counters.SetCounter(DepositCounter.AverageAmountAllDepositsUserEur14d, avgAmount * (1 + (float)(_random.NextDouble() * 3 - 1.5)));
        counters.SetCounter(DepositCounter.AverageAmountAllDepositsUserEur30d, avgAmount * (1 + (float)(_random.NextDouble() * 4 - 2)));
        counters.SetCounter(DepositCounter.AverageAmountAllDepositsUserEur60d, avgAmount * (1 + (float)(_random.NextDouble() * 5 - 2.5)));

        // Success amounts
        counters.SetCounter(DepositCounter.AverageAmountAllSuccessDepositsEurUser1d, avgAmount * 0.5f);
        counters.SetCounter(DepositCounter.AverageAmountAllSuccessDepositsEurUser7d, avgAmount * 0.4f);
        counters.SetCounter(DepositCounter.AverageAmountAllSuccessDepositsEurUser14d, avgAmount * 0.3f);
        counters.SetCounter(DepositCounter.AverageAmountAllSuccessDepositsEurUser30d, avgAmount * 0.25f);
        counters.SetCounter(DepositCounter.AverageAmountAllSuccessDepositsEurUser60d, avgAmount * 0.2f);

        // Transaction counts - very high
        counters.SetCounter(DepositCounter.CountTransactionsUser1h, _random.Next(10, 50));
        counters.SetCounter(DepositCounter.CountPendingTransactionsUser1h, _random.Next(3, 10));
        counters.SetCounter(DepositCounter.CountPendingTransactionsUser1d, _random.Next(10, 30));
        counters.SetCounter(DepositCounter.CountSuccessPayments30dInRow, _random.Next(0, 3));

        // Same amount transactions - high (testing cards)
        counters.SetCounter(DepositCounter.CountTransactionsWithSameAmountInRowUser1d, _random.Next(5, 15));
        counters.SetCounter(DepositCounter.CountTransactionsWithSameAmountInRowUser7d, _random.Next(10, 30));
        counters.SetCounter(DepositCounter.CountTransactionsWithSameAmountInRowUser14d, _random.Next(15, 50));
        counters.SetCounter(DepositCounter.CountTransactionsWithSameAmountInRowUser30d, _random.Next(20, 80));

        // Success payments - low history
        counters.SetCounter(DepositCounter.CountSuccessPaymentsUser1d, _random.Next(0, 3));
        counters.SetCounter(DepositCounter.CountSuccessPaymentsUser7d, _random.Next(0, 5));
        counters.SetCounter(DepositCounter.CountSuccessPaymentsUser14d, _random.Next(0, 8));
        counters.SetCounter(DepositCounter.CountSuccessPaymentsUser30d, _random.Next(0, 10));
        counters.SetCounter(DepositCounter.CountSuccessPayments60d, _random.Next(0, 15));

        // Success to fail ratio - low
        counters.SetCounter(DepositCounter.CountSuccessToFailTransactionsRatioUser1h, _random.Next(10, 40) / 100f);
        counters.SetCounter(DepositCounter.CountSuccessToFailTransactionsRatioUser1d, _random.Next(15, 45) / 100f);
        counters.SetCounter(DepositCounter.CountSuccessToFailTransactionsRatioUser7d, _random.Next(20, 50) / 100f);
        counters.SetCounter(DepositCounter.CountSuccessToFailTransactionsRatioUser14d, _random.Next(20, 50) / 100f);
        counters.SetCounter(DepositCounter.CountSuccessToFailTransactionsRatioUser30d, _random.Next(25, 55) / 100f);

        // Deposit to payout ratio - very high (money laundering pattern)
        counters.SetCounter(DepositCounter.PercentAmountSuccessDepositToPayoutUser1d, _random.Next(90, 100) / 100f);
        counters.SetCounter(DepositCounter.PercentAmountSuccessDepositToPayoutUser7d, _random.Next(85, 100) / 100f);
        counters.SetCounter(DepositCounter.PercentAmountSuccessDepositToPayoutUser14d, _random.Next(80, 100) / 100f);
        counters.SetCounter(DepositCounter.PercentAmountSuccessDepositToPayoutUser30d, _random.Next(80, 100) / 100f);
        counters.SetCounter(DepositCounter.PercentAmountSuccessDepositToPayoutUser60d, _random.Next(75, 100) / 100f);

        // Payment methods - many
        counters.SetCounter(DepositCounter.CountUniquePaymentMethodsUser1h, _random.Next(2, 5));
        counters.SetCounter(DepositCounter.CountUniquePaymentMethodsUser1d, _random.Next(3, 8));
        counters.SetCounter(DepositCounter.CountUniquePaymentMethodsUser7d, _random.Next(5, 12));
        counters.SetCounter(DepositCounter.CountUniquePaymentMethodsUser14d, _random.Next(8, 15));
        counters.SetCounter(DepositCounter.CountUniquePaymentMethodsUser30d, _random.Next(10, 20));

        // Time since last deposit - very recent
        counters.SetCounter(DepositCounter.CountTimeSinceLastDepositInSecondsUser, _random.Next(10, 300));
        counters.SetCounter(DepositCounter.CountTimeSinceLastFailedDepositInSecondsUser, _random.Next(60, 600));
        counters.SetCounter(DepositCounter.CountTimeSinceLastSuccessDepositInSecondsUser, _random.Next(300, 3600));
        counters.SetCounter(DepositCounter.CountAmountDeviationRatioFromUserAvgUser, (float)(_random.NextDouble() * 2 + 0.5));

        // Card sums - very high
        counters.SetCounter(DepositCounter.SumSuccessDepositsCard1h, _random.Next(1000, 5000));
        counters.SetCounter(DepositCounter.SumSuccessDepositsCard1d, _random.Next(5000, 20000));
        counters.SetCounter(DepositCounter.SumSuccessDepositsCard7d, _random.Next(10000, 50000));
        counters.SetCounter(DepositCounter.SumSuccessDepositsCard14d, _random.Next(20000, 100000));
        counters.SetCounter(DepositCounter.SumSuccessDepositsCard30d, _random.Next(50000, 200000));
        counters.SetCounter(DepositCounter.SumSuccessDepositsCard60d, _random.Next(100000, 500000));

        // Country mismatch - frequent
        counters.SetCounter(DepositCounter.IsTransactionCountryMismatch, _random.NextDouble() < 0.6 ? 1 : 0);
        counters.SetCounter(DepositCounter.HourOfDay, _random.Next(0, 24)); // Any hour including suspicious
        counters.SetCounter(DepositCounter.DayOfWeek, _random.Next(0, 7));

        // User history - new account
        counters.SetCounter(DepositCounter.CountUserCountCountryMismatchTransactionsUser, _random.Next(5, 20));
        counters.SetCounter(DepositCounter.CountUserTimeFromRegistrationToFirstDepInSeconds, _random.Next(60, 3600));
        counters.SetCounter(DepositCounter.CountUserTimeFromFirstSuccessDepInSeconds30d, _random.Next(0, 86400));
        counters.SetCounter(DepositCounter.CountUserTimeFromRegistrationInSeconds, _random.Next(3600, 86400));

        // Antifraud declines - high
        counters.SetCounter(DepositCounter.CountAntifraudDeclinesUser1h, _random.Next(2, 8));
        counters.SetCounter(DepositCounter.CountAntifraudDeclinesUser1d, _random.Next(5, 15));
        counters.SetCounter(DepositCounter.CountAntifraudDeclinesUser7d, _random.Next(10, 30));
        counters.SetCounter(DepositCounter.CountAntifraudDeclinesUser14d, _random.Next(15, 50));
        counters.SetCounter(DepositCounter.CountAntifraudDeclinesUser30d, _random.Next(20, 80));

        // Document counters - many cards per document
        counters.SetCounter(DepositCounter.CountCardsDocument1d, _random.Next(3, 10));
        counters.SetCounter(DepositCounter.CountCardsDocument7d, _random.Next(8, 20));
        counters.SetCounter(DepositCounter.CountCardsDocument30d, _random.Next(15, 40));

        // IP session counters - high sharing
        SetFraudIpCounters(counters);
        SetFraudEmailCounters(counters);
        SetFraudCardCounters(counters);
        SetFraudUserIpCounters(counters);
    }


    private static void SetLegitimateIpCounters(TransactionCounters counters)
    {
        counters.SetCounter(DepositCounter.CountBillingCountriesIp1d, 1);
        counters.SetCounter(DepositCounter.CountBillingCountriesIp7d, _random.Next(1, 2));
        counters.SetCounter(DepositCounter.CountBillingCountriesIp30d, _random.Next(1, 3));
        counters.SetCounter(DepositCounter.CountDocsIp1d, _random.Next(1, 2));
        counters.SetCounter(DepositCounter.CountDocsIp7d, _random.Next(1, 3));
        counters.SetCounter(DepositCounter.CountDocsIp30d, _random.Next(1, 4));
        counters.SetCounter(DepositCounter.CountDevicesIp1d, _random.Next(1, 2));
        counters.SetCounter(DepositCounter.CountDevicesIp7d, _random.Next(1, 3));
        counters.SetCounter(DepositCounter.CountDevicesIp30d, _random.Next(1, 4));
        counters.SetCounter(DepositCounter.CountDeviceFingerprintsIp1d, _random.Next(1, 2));
        counters.SetCounter(DepositCounter.CountDeviceFingerprintsIp7d, _random.Next(1, 3));
        counters.SetCounter(DepositCounter.CountDeviceFingerprintsIp30d, _random.Next(1, 4));
        counters.SetCounter(DepositCounter.CountCardsIp1d, _random.Next(1, 2));
        counters.SetCounter(DepositCounter.CountCardsIp7d, _random.Next(1, 3));
        counters.SetCounter(DepositCounter.CountCardsIp30d, _random.Next(1, 4));
        counters.SetCounter(DepositCounter.CountPhonesIp1d, _random.Next(1, 2));
        counters.SetCounter(DepositCounter.CountPhonesIp7d, _random.Next(1, 3));
        counters.SetCounter(DepositCounter.CountPhonesIp30d, _random.Next(1, 4));
        counters.SetCounter(DepositCounter.CountEmailsIp1d, _random.Next(1, 2));
        counters.SetCounter(DepositCounter.CountEmailsIp7d, _random.Next(1, 3));
        counters.SetCounter(DepositCounter.CountEmailsIp30d, _random.Next(1, 4));
        counters.SetCounter(DepositCounter.CountUsersIp1d, _random.Next(1, 2));
        counters.SetCounter(DepositCounter.CountUsersIp7d, _random.Next(1, 3));
        counters.SetCounter(DepositCounter.CountUsersWallet30d, _random.Next(1, 3));
        counters.SetCounter(DepositCounter.AverageSecondsBetweenTransactionsIp10m, _random.Next(60, 600));
        counters.SetCounter(DepositCounter.AverageSecondsBetweenTransactionsIp30m, _random.Next(120, 1800));
        counters.SetCounter(DepositCounter.AverageSecondsBetweenTransactionsIp1h, _random.Next(300, 3600));
        counters.SetCounter(DepositCounter.AverageSecondsBetweenTransactionsIp1d, _random.Next(3600, 86400));
    }

    private static void SetFraudIpCounters(TransactionCounters counters)
    {
        counters.SetCounter(DepositCounter.CountBillingCountriesIp1d, _random.Next(3, 8));
        counters.SetCounter(DepositCounter.CountBillingCountriesIp7d, _random.Next(5, 15));
        counters.SetCounter(DepositCounter.CountBillingCountriesIp30d, _random.Next(10, 25));
        counters.SetCounter(DepositCounter.CountDocsIp1d, _random.Next(5, 15));
        counters.SetCounter(DepositCounter.CountDocsIp7d, _random.Next(10, 30));
        counters.SetCounter(DepositCounter.CountDocsIp30d, _random.Next(20, 60));
        counters.SetCounter(DepositCounter.CountDevicesIp1d, _random.Next(5, 15));
        counters.SetCounter(DepositCounter.CountDevicesIp7d, _random.Next(10, 30));
        counters.SetCounter(DepositCounter.CountDevicesIp30d, _random.Next(20, 60));
        counters.SetCounter(DepositCounter.CountDeviceFingerprintsIp1d, _random.Next(5, 15));
        counters.SetCounter(DepositCounter.CountDeviceFingerprintsIp7d, _random.Next(10, 30));
        counters.SetCounter(DepositCounter.CountDeviceFingerprintsIp30d, _random.Next(20, 60));
        counters.SetCounter(DepositCounter.CountCardsIp1d, _random.Next(5, 20));
        counters.SetCounter(DepositCounter.CountCardsIp7d, _random.Next(15, 50));
        counters.SetCounter(DepositCounter.CountCardsIp30d, _random.Next(30, 100));
        counters.SetCounter(DepositCounter.CountPhonesIp1d, _random.Next(3, 10));
        counters.SetCounter(DepositCounter.CountPhonesIp7d, _random.Next(8, 25));
        counters.SetCounter(DepositCounter.CountPhonesIp30d, _random.Next(15, 50));
        counters.SetCounter(DepositCounter.CountEmailsIp1d, _random.Next(5, 15));
        counters.SetCounter(DepositCounter.CountEmailsIp7d, _random.Next(15, 40));
        counters.SetCounter(DepositCounter.CountEmailsIp30d, _random.Next(30, 80));
        counters.SetCounter(DepositCounter.CountUsersIp1d, _random.Next(5, 20));
        counters.SetCounter(DepositCounter.CountUsersIp7d, _random.Next(15, 50));
        counters.SetCounter(DepositCounter.CountUsersWallet30d, _random.Next(10, 40));
        counters.SetCounter(DepositCounter.AverageSecondsBetweenTransactionsIp10m, _random.Next(5, 30));
        counters.SetCounter(DepositCounter.AverageSecondsBetweenTransactionsIp30m, _random.Next(10, 60));
        counters.SetCounter(DepositCounter.AverageSecondsBetweenTransactionsIp1h, _random.Next(20, 120));
        counters.SetCounter(DepositCounter.AverageSecondsBetweenTransactionsIp1d, _random.Next(60, 600));
    }

    private static void SetLegitimateEmailCounters(TransactionCounters counters)
    {
        counters.SetCounter(DepositCounter.CountDevicesEmail1d, _random.Next(1, 2));
        counters.SetCounter(DepositCounter.CountDevicesEmail7d, _random.Next(1, 3));
        counters.SetCounter(DepositCounter.CountDevicesEmail30d, _random.Next(1, 4));
        counters.SetCounter(DepositCounter.CountCardsEmail1d, _random.Next(1, 2));
        counters.SetCounter(DepositCounter.CountCardsEmail7d, _random.Next(1, 3));
        counters.SetCounter(DepositCounter.CountCardsEmail30d, _random.Next(1, 4));
        counters.SetCounter(DepositCounter.CountSimilarEmails30d, _random.Next(0, 2));
    }

    private static void SetFraudEmailCounters(TransactionCounters counters)
    {
        counters.SetCounter(DepositCounter.CountDevicesEmail1d, _random.Next(3, 10));
        counters.SetCounter(DepositCounter.CountDevicesEmail7d, _random.Next(8, 25));
        counters.SetCounter(DepositCounter.CountDevicesEmail30d, _random.Next(15, 50));
        counters.SetCounter(DepositCounter.CountCardsEmail1d, _random.Next(3, 10));
        counters.SetCounter(DepositCounter.CountCardsEmail7d, _random.Next(8, 25));
        counters.SetCounter(DepositCounter.CountCardsEmail30d, _random.Next(15, 50));
        counters.SetCounter(DepositCounter.CountSimilarEmails30d, _random.Next(5, 20));
    }

    private static void SetLegitimateCardCounters(TransactionCounters counters)
    {
        counters.SetCounter(DepositCounter.CountEmailsCard1d, _random.Next(1, 2));
        counters.SetCounter(DepositCounter.CountEmailsCard7d, _random.Next(1, 2));
        counters.SetCounter(DepositCounter.CountIpCountriesCard1d, 1);
        counters.SetCounter(DepositCounter.CountIpCountriesCard7d, _random.Next(1, 2));
        counters.SetCounter(DepositCounter.CountIpCountriesCard30d, _random.Next(1, 3));
        counters.SetCounter(DepositCounter.CountDevicesCard1d, _random.Next(1, 2));
        counters.SetCounter(DepositCounter.CountDevicesCard7d, _random.Next(1, 3));
        counters.SetCounter(DepositCounter.CountDevicesCard30d, _random.Next(1, 4));
        counters.SetCounter(DepositCounter.CountUsersCard1d, 1);
        counters.SetCounter(DepositCounter.CountUsersCard7d, 1);
        counters.SetCounter(DepositCounter.CountUsersCard14d, 1);
        counters.SetCounter(DepositCounter.CountSuccessDepUserCard30d, _random.Next(5, 30));
        counters.SetCounter(DepositCounter.CountFailDepCard1d, _random.Next(0, 2));
    }

    private static void SetFraudCardCounters(TransactionCounters counters)
    {
        counters.SetCounter(DepositCounter.CountEmailsCard1d, _random.Next(3, 10));
        counters.SetCounter(DepositCounter.CountEmailsCard7d, _random.Next(8, 25));
        counters.SetCounter(DepositCounter.CountIpCountriesCard1d, _random.Next(2, 5));
        counters.SetCounter(DepositCounter.CountIpCountriesCard7d, _random.Next(4, 10));
        counters.SetCounter(DepositCounter.CountIpCountriesCard30d, _random.Next(8, 20));
        counters.SetCounter(DepositCounter.CountDevicesCard1d, _random.Next(3, 10));
        counters.SetCounter(DepositCounter.CountDevicesCard7d, _random.Next(8, 25));
        counters.SetCounter(DepositCounter.CountDevicesCard30d, _random.Next(15, 50));
        counters.SetCounter(DepositCounter.CountUsersCard1d, _random.Next(2, 8));
        counters.SetCounter(DepositCounter.CountUsersCard7d, _random.Next(5, 15));
        counters.SetCounter(DepositCounter.CountUsersCard14d, _random.Next(8, 25));
        counters.SetCounter(DepositCounter.CountSuccessDepUserCard30d, _random.Next(0, 5));
        counters.SetCounter(DepositCounter.CountFailDepCard1d, _random.Next(5, 20));
    }

    private static void SetLegitimateUserIpCounters(TransactionCounters counters)
    {
        counters.SetCounter(DepositCounter.CountUsersUserIp1d, _random.Next(1, 2));
        counters.SetCounter(DepositCounter.CountUsersUserIp30d, _random.Next(1, 3));
        counters.SetCounter(DepositCounter.AverageSecondsBetweenTransactionsUserIp10m, _random.Next(60, 600));
        counters.SetCounter(DepositCounter.AverageSecondsBetweenTransactionsUserIp30m, _random.Next(120, 1800));
        counters.SetCounter(DepositCounter.AverageSecondsBetweenTransactionsUserIp1h, _random.Next(300, 3600));
        counters.SetCounter(DepositCounter.AverageSecondsBetweenTransactionsUserIp1d, _random.Next(3600, 86400));
    }

    private static void SetFraudUserIpCounters(TransactionCounters counters)
    {
        counters.SetCounter(DepositCounter.CountUsersUserIp1d, _random.Next(5, 20));
        counters.SetCounter(DepositCounter.CountUsersUserIp30d, _random.Next(20, 80));
        counters.SetCounter(DepositCounter.AverageSecondsBetweenTransactionsUserIp10m, _random.Next(5, 30));
        counters.SetCounter(DepositCounter.AverageSecondsBetweenTransactionsUserIp30m, _random.Next(10, 60));
        counters.SetCounter(DepositCounter.AverageSecondsBetweenTransactionsUserIp1h, _random.Next(20, 120));
        counters.SetCounter(DepositCounter.AverageSecondsBetweenTransactionsUserIp1d, _random.Next(60, 600));
    }

    private static decimal GenerateLegitimateAmount()
    {
        // Normal distribution around common amounts
        var amounts = new[] { 10, 20, 50, 100, 200, 500 };
        var baseAmount = amounts[_random.Next(amounts.Length)];
        var variation = (decimal)(_random.NextDouble() * 0.2 - 0.1); // ±10%
        return Math.Round(baseAmount * (1 + variation), 2);
    }

    private static decimal GenerateFraudAmount()
    {
        // Unusual amounts - either very small (testing) or very large
        if (_random.NextDouble() < 0.3)
        {
            // Small test amounts
            return Math.Round((decimal)(_random.NextDouble() * 5 + 0.01), 2);
        }
        else
        {
            // Large amounts
            return Math.Round((decimal)(_random.NextDouble() * 5000 + 1000), 2);
        }
    }

    private static void ShuffleData(Transaction[] transactions, bool[] labels)
    {
        int n = transactions.Length;
        for (int i = n - 1; i > 0; i--)
        {
            int j = _random.Next(i + 1);
            (transactions[i], transactions[j]) = (transactions[j], transactions[i]);
            (labels[i], labels[j]) = (labels[j], labels[i]);
        }
    }
}
