namespace FraudDetection.Core.Models;

/// <summary>
/// Counter names for deposit transactions stored in PostgreSQL as JSON.
/// </summary>
public static class DepositCounter
{
    #region Phone
    public const string CountIpCountriesPhone1d = "count_ipCountries_phone_1d";
    public const string CountIpCountriesPhone7d = "count_ipCountries_phone_7d";
    public const string CountIpCountriesPhone30d = "count_ipCountries_phone_30d";
    public const string CountDevicesPhone1d = "count_devices_phone_1d";
    public const string CountDevicesPhone7d = "count_devices_phone_7d";
    public const string CountDevicesPhone30d = "count_devices_phone_30d";
    public const string CountCardsPhone1d = "count_cards_phone_1d";
    public const string CountCardsPhone7d = "count_cards_phone_7d";
    public const string CountCardsPhone30d = "count_cards_phone_30d";
    #endregion

    #region UserId
    public const string CountFailsTransUser1h = "count_fails_trans_user_1h";
    public const string CountFailsTransUser1hInRow = "count_fails_trans_user_1h_in_row";
    public const string CountFailsTransUser3dInRow = "count_fails_trans_user_3d_in_row";
    public const string CountCardsUser1d = "count_cards_user_1d";
    public const string CountCardsUser7d = "count_cards_user_7d";
    public const string CountCardsUser14d = "count_cards_user_14d";
    public const string CountCardsUser30d = "count_cards_user_30d";
    public const string CountDeviceFingerprintsUser1d = "count_deviceFingerprints_user_1d";
    public const string CountDeviceFingerprintsUser7d = "count_deviceFingerprints_user_7d";
    public const string CountDeviceFingerprintsUser30d = "count_deviceFingerprints_user_30d";
    public const string CountPhonesUser1d = "count_phones_user_1d";
    public const string CountPhonesUser7d = "count_phones_user_7d";
    public const string CountPhonesUser14d = "count_phones_user_14d";
    public const string CountLowRiskDeclinesUser1d = "count_low_risk_declines_user_1d";
    public const string CountMediumRiskDeclinesUser1d = "count_medium_risk_declines_user_1d";
    public const string CountHighRiskDeclinesUser1d = "count_high_risk_declines_user_1d";
    public const string CountIpUser1d = "count_ip_user_1d";
    public const string CountIpUser7d = "count_ip_user_7d";
    public const string CountIpUser14d = "count_ip_user_14d";
    public const string CountIpCountriesUser1d = "count_ipCountries_user_1d";
    public const string CountIpCountriesUser7d = "count_ipCountries_user_7d";
    public const string CountIpCountriesUser14d = "count_ipCountries_user_14d";
    public const string CountEmailsUser1d = "count_emails_user_1d";
    public const string CountEmailsUser7d = "count_emails_user_7d";
    public const string CountEmailsUser14d = "count_emails_user_14d";
    public const string CountEmailsUser30d = "count_emails_user_30d";
    public const string CountWalletsUser30d = "count_wallets_user_30d";

    public const string AverageSecondsBetweenTransactionsUser10m = "average_seconds_between_transactions_user_10m";
    public const string AverageSecondsBetweenTransactionsUser30m = "average_seconds_between_transactions_user_30m";
    public const string AverageSecondsBetweenTransactionsUser1h = "average_seconds_between_transactions_user_1h";
    public const string AverageSecondsBetweenTransactionsUser1d = "average_seconds_between_transactions_user_1d";

    public const string AverageAmountAllDepositsUserEur1d = "average_amount_all_deposits_user_eur_1d";
    public const string AverageAmountAllDepositsUserEur7d = "average_amount_all_deposits_user_eur_7d";
    public const string AverageAmountAllDepositsUserEur14d = "average_amount_all_deposits_user_eur_14d";
    public const string AverageAmountAllDepositsUserEur30d = "average_amount_all_deposits_user_eur_30d";
    public const string AverageAmountAllDepositsUserEur60d = "average_amount_all_deposits_user_eur_60d";

    public const string AverageAmountAllSuccessDepositsEurUser1d = "average_amount_all_success_deposits_eur_user_1d";
    public const string AverageAmountAllSuccessDepositsEurUser7d = "average_amount_all_success_deposits_eur_user_7d";
    public const string AverageAmountAllSuccessDepositsEurUser14d = "average_amount_all_success_deposits_eur_user_14d";
    public const string AverageAmountAllSuccessDepositsEurUser30d = "average_amount_all_success_deposits_eur_user_30d";
    public const string AverageAmountAllSuccessDepositsEurUser60d = "average_amount_all_success_deposits_eur_user_60d";

    public const string CountTransactionsUser1h = "count_transactions_user_1h";
    public const string CountPendingTransactionsUser1h = "count_pending_transactions_user_1h";
    public const string CountPendingTransactionsUser1d = "count_pending_transactions_user_1d";

    public const string CountSuccessPayments30dInRow = "count_success_payments_30d_in_row";

    public const string CountTransactionsWithSameAmountInRowUser1d = "count_transactions_with_same_amount_in_row_user_1d";
    public const string CountTransactionsWithSameAmountInRowUser7d = "count_transactions_with_same_amount_in_row_user_7d";
    public const string CountTransactionsWithSameAmountInRowUser14d = "count_transactions_with_same_amount_in_row_user_14d";
    public const string CountTransactionsWithSameAmountInRowUser30d = "count_transactions_with_same_amount_in_row_user_30d";

    public const string CountSuccessPaymentsUser1d = "count_success_payments_user_1d";
    public const string CountSuccessPaymentsUser7d = "count_success_payments_user_7d";
    public const string CountSuccessPaymentsUser14d = "count_success_payments_user_14d";
    public const string CountSuccessPaymentsUser30d = "count_success_payments_user_30d";
    public const string CountSuccessPayments60d = "count_success_payments_60d";

    public const string CountSuccessToFailTransactionsRatioUser1h = "count_success_to_fail_transactions_ratio_user_1h";
    public const string CountSuccessToFailTransactionsRatioUser1d = "count_success_to_fail_transactions_ratio_user_1d";
    public const string CountSuccessToFailTransactionsRatioUser7d = "count_success_to_fail_transactions_ratio_user_7d";
    public const string CountSuccessToFailTransactionsRatioUser14d = "count_success_to_fail_transactions_ratio_user_14d";
    public const string CountSuccessToFailTransactionsRatioUser30d = "count_success_to_fail_transactions_ratio_user_30d";

    public const string PercentAmountSuccessDepositToPayoutUser1d = "percent_amount_success_deposit_to_payout_user_1d";
    public const string PercentAmountSuccessDepositToPayoutUser7d = "percent_amount_success_deposit_to_payout_user_7d";
    public const string PercentAmountSuccessDepositToPayoutUser14d = "percent_amount_success_deposit_to_payout_user_14d";
    public const string PercentAmountSuccessDepositToPayoutUser30d = "percent_amount_success_deposit_to_payout_user_30d";
    public const string PercentAmountSuccessDepositToPayoutUser60d = "percent_amount_success_deposit_to_payout_user_60d";

    public const string CountUniquePaymentMethodsUser1h = "count_unique_payment_methods_user_1h";
    public const string CountUniquePaymentMethodsUser1d = "count_unique_payment_methods_user_1d";
    public const string CountUniquePaymentMethodsUser7d = "count_unique_payment_methods_user_7d";
    public const string CountUniquePaymentMethodsUser14d = "count_unique_payment_methods_user_14d";
    public const string CountUniquePaymentMethodsUser30d = "count_unique_payment_methods_user_30d";

    public const string CountTimeSinceLastDepositInSecondsUser = "count_time_since_last_deposit_in_seconds_user";
    public const string CountTimeSinceLastFailedDepositInSecondsUser = "count_time_since_last_failed_deposit_in_seconds_user";
    public const string CountTimeSinceLastSuccessDepositInSecondsUser = "count_time_since_last_success_deposit_in_seconds_user";
    public const string CountAmountDeviationRatioFromUserAvgUser = "count_amount_deviation_ratio_from_user_avg_user";

    public const string SumSuccessDepositsCard1h = "sum_success_deposits_card_1h";
    public const string SumSuccessDepositsCard1d = "sum_success_deposits_card_1d";
    public const string SumSuccessDepositsCard7d = "sum_success_deposits_card_7d";
    public const string SumSuccessDepositsCard14d = "sum_success_deposits_card_14d";
    public const string SumSuccessDepositsCard30d = "sum_success_deposits_card_30d";
    public const string SumSuccessDepositsCard60d = "sum_success_deposits_card_60d";

    public const string IsTransactionCountryMismatch = "is_transaction_country_mismatch";

    public const string HourOfDay = "hour_of_day";
    public const string DayOfWeek = "day_of_week";

    public const string CountUserCountCountryMismatchTransactionsUser = "count_user_count_country_mismatch_transactions_user";
    public const string CountUserTimeFromRegistrationToFirstDepInSeconds = "count_user_time_from_registration_to_first_dep_in_seconds";
    public const string CountUserTimeFromFirstSuccessDepInSeconds30d = "count_user_time_from_first_success_dep_in_seconds_30d";
    public const string CountUserTimeFromRegistrationInSeconds = "count_user_time_from_registration_in_seconds";

    public const string CountAntifraudDeclinesUser1h = "count_antifraud_declines_user_1h";
    public const string CountAntifraudDeclinesUser1d = "count_antifraud_declines_user_1d";
    public const string CountAntifraudDeclinesUser7d = "count_antifraud_declines_user_7d";
    public const string CountAntifraudDeclinesUser14d = "count_antifraud_declines_user_14d";
    public const string CountAntifraudDeclinesUser30d = "count_antifraud_declines_user_30d";
    #endregion

    #region Document
    public const string CountCardsDocument1d = "count_cards_doc_1d";
    public const string CountCardsDocument7d = "count_cards_doc_7d";
    public const string CountCardsDocument30d = "count_cards_doc_30d";
    #endregion

    #region SessionIp
    public const string CountBillingCountriesIp1d = "count_billingCountries_ip_1d";
    public const string CountBillingCountriesIp7d = "count_billingCountries_ip_7d";
    public const string CountBillingCountriesIp30d = "count_billingCountries_ip_30d";
    public const string CountDocsIp1d = "count_docs_ip_1d";
    public const string CountDocsIp7d = "count_docs_ip_7d";
    public const string CountDocsIp30d = "count_docs_ip_30d";
    public const string CountDevicesIp1d = "count_devices_ip_1d";
    public const string CountDevicesIp7d = "count_devices_ip_7d";
    public const string CountDevicesIp30d = "count_devices_ip_30d";
    public const string CountDeviceFingerprintsIp1d = "count_deviceFingerprints_ip_1d";
    public const string CountDeviceFingerprintsIp7d = "count_deviceFingerprints_ip_7d";
    public const string CountDeviceFingerprintsIp30d = "count_deviceFingerprints_ip_30d";
    public const string CountCardsIp1d = "count_cards_ip_1d";
    public const string CountCardsIp7d = "count_cards_ip_7d";
    public const string CountCardsIp30d = "count_cards_ip_30d";
    public const string CountPhonesIp1d = "count_phones_ip_1d";
    public const string CountPhonesIp7d = "count_phones_ip_7d";
    public const string CountPhonesIp30d = "count_phones_ip_30d";
    public const string CountEmailsIp1d = "count_emails_ip_1d";
    public const string CountEmailsIp7d = "count_emails_ip_7d";
    public const string CountEmailsIp30d = "count_emails_ip_30d";
    public const string CountUsersIp1d = "count_users_ip_1d";
    public const string CountUsersIp7d = "count_users_ip_7d";
    public const string CountUsersWallet30d = "count_users_wallet_30d";

    public const string AverageSecondsBetweenTransactionsIp10m = "average_seconds_between_transactions_Ip_10m";
    public const string AverageSecondsBetweenTransactionsIp30m = "average_seconds_between_transactions_Ip_30m";
    public const string AverageSecondsBetweenTransactionsIp1h = "average_seconds_between_transactions_Ip_1h";
    public const string AverageSecondsBetweenTransactionsIp1d = "average_seconds_between_transactions_Ip_1d";
    #endregion

    #region Email
    public const string CountDevicesEmail1d = "count_devices_email_1d";
    public const string CountDevicesEmail7d = "count_devices_email_7d";
    public const string CountDevicesEmail30d = "count_devices_email_30d";
    public const string CountCardsEmail1d = "count_cards_email_1d";
    public const string CountCardsEmail7d = "count_cards_email_7d";
    public const string CountCardsEmail30d = "count_cards_email_30d";
    public const string CountSimilarEmails30d = "count_similar_emails_30d";
    #endregion

    #region Card
    public const string CountEmailsCard1d = "count_emails_card_1d";
    public const string CountEmailsCard7d = "count_emails_card_7d";
    public const string CountIpCountriesCard1d = "count_ipCountries_card_1d";
    public const string CountIpCountriesCard7d = "count_ipCountries_card_7d";
    public const string CountIpCountriesCard30d = "count_ipCountries_card_30d";
    public const string CountDevicesCard1d = "count_devices_card_1d";
    public const string CountDevicesCard7d = "count_devices_card_7d";
    public const string CountDevicesCard30d = "count_devices_card_30d";
    public const string CountUsersCard1d = "count_users_card_1d";
    public const string CountUsersCard7d = "count_users_card_7d";
    public const string CountUsersCard14d = "count_users_card_14d";
    public const string CountSuccessDepUserCard30d = "count_success_dep_user_card_30d";
    public const string CountFailDepCard1d = "count_card_fails_1d";
    #endregion

    #region UserIP
    public const string CountUsersUserIp1d = "count_users_userIp_1d";
    public const string CountUsersUserIp30d = "count_users_userIp_30d";

    public const string AverageSecondsBetweenTransactionsUserIp10m = "average_seconds_between_transactions_userIp_10m";
    public const string AverageSecondsBetweenTransactionsUserIp30m = "average_seconds_between_transactions_userIp_30m";
    public const string AverageSecondsBetweenTransactionsUserIp1h = "average_seconds_between_transactions_userIp_1h";
    public const string AverageSecondsBetweenTransactionsUserIp1d = "average_seconds_between_transactions_userIp_1d";
    #endregion

    /// <summary>
    /// Gets all counter names in a fixed order for feature extraction.
    /// </summary>
    public static readonly string[] AllCounterNames = new[]
    {
        // Phone counters (9)
        CountIpCountriesPhone1d, CountIpCountriesPhone7d, CountIpCountriesPhone30d,
        CountDevicesPhone1d, CountDevicesPhone7d, CountDevicesPhone30d,
        CountCardsPhone1d, CountCardsPhone7d, CountCardsPhone30d,
        
        // UserId counters (77)
        CountFailsTransUser1h, CountFailsTransUser1hInRow, CountFailsTransUser3dInRow,
        CountCardsUser1d, CountCardsUser7d, CountCardsUser14d, CountCardsUser30d,
        CountDeviceFingerprintsUser1d, CountDeviceFingerprintsUser7d, CountDeviceFingerprintsUser30d,
        CountPhonesUser1d, CountPhonesUser7d, CountPhonesUser14d,
        CountLowRiskDeclinesUser1d, CountMediumRiskDeclinesUser1d, CountHighRiskDeclinesUser1d,
        CountIpUser1d, CountIpUser7d, CountIpUser14d,
        CountIpCountriesUser1d, CountIpCountriesUser7d, CountIpCountriesUser14d,
        CountEmailsUser1d, CountEmailsUser7d, CountEmailsUser14d, CountEmailsUser30d,
        CountWalletsUser30d,
        AverageSecondsBetweenTransactionsUser10m, AverageSecondsBetweenTransactionsUser30m,
        AverageSecondsBetweenTransactionsUser1h, AverageSecondsBetweenTransactionsUser1d,
        AverageAmountAllDepositsUserEur1d, AverageAmountAllDepositsUserEur7d,
        AverageAmountAllDepositsUserEur14d, AverageAmountAllDepositsUserEur30d, AverageAmountAllDepositsUserEur60d,
        AverageAmountAllSuccessDepositsEurUser1d, AverageAmountAllSuccessDepositsEurUser7d,
        AverageAmountAllSuccessDepositsEurUser14d, AverageAmountAllSuccessDepositsEurUser30d, AverageAmountAllSuccessDepositsEurUser60d,
        CountTransactionsUser1h, CountPendingTransactionsUser1h, CountPendingTransactionsUser1d,
        CountSuccessPayments30dInRow,
        CountTransactionsWithSameAmountInRowUser1d, CountTransactionsWithSameAmountInRowUser7d,
        CountTransactionsWithSameAmountInRowUser14d, CountTransactionsWithSameAmountInRowUser30d,
        CountSuccessPaymentsUser1d, CountSuccessPaymentsUser7d, CountSuccessPaymentsUser14d, CountSuccessPaymentsUser30d, CountSuccessPayments60d,
        CountSuccessToFailTransactionsRatioUser1h, CountSuccessToFailTransactionsRatioUser1d,
        CountSuccessToFailTransactionsRatioUser7d, CountSuccessToFailTransactionsRatioUser14d, CountSuccessToFailTransactionsRatioUser30d,
        PercentAmountSuccessDepositToPayoutUser1d, PercentAmountSuccessDepositToPayoutUser7d,
        PercentAmountSuccessDepositToPayoutUser14d, PercentAmountSuccessDepositToPayoutUser30d, PercentAmountSuccessDepositToPayoutUser60d,
        CountUniquePaymentMethodsUser1h, CountUniquePaymentMethodsUser1d, CountUniquePaymentMethodsUser7d,
        CountUniquePaymentMethodsUser14d, CountUniquePaymentMethodsUser30d,
        CountTimeSinceLastDepositInSecondsUser, CountTimeSinceLastFailedDepositInSecondsUser, CountTimeSinceLastSuccessDepositInSecondsUser,
        CountAmountDeviationRatioFromUserAvgUser,
        SumSuccessDepositsCard1h, SumSuccessDepositsCard1d, SumSuccessDepositsCard7d,
        SumSuccessDepositsCard14d, SumSuccessDepositsCard30d, SumSuccessDepositsCard60d,
        IsTransactionCountryMismatch, HourOfDay, DayOfWeek,
        CountUserCountCountryMismatchTransactionsUser, CountUserTimeFromRegistrationToFirstDepInSeconds,
        CountUserTimeFromFirstSuccessDepInSeconds30d, CountUserTimeFromRegistrationInSeconds,
        CountAntifraudDeclinesUser1h, CountAntifraudDeclinesUser1d, CountAntifraudDeclinesUser7d,
        CountAntifraudDeclinesUser14d, CountAntifraudDeclinesUser30d,
        
        // Document counters (3)
        CountCardsDocument1d, CountCardsDocument7d, CountCardsDocument30d,
        
        // SessionIp counters (27)
        CountBillingCountriesIp1d, CountBillingCountriesIp7d, CountBillingCountriesIp30d,
        CountDocsIp1d, CountDocsIp7d, CountDocsIp30d,
        CountDevicesIp1d, CountDevicesIp7d, CountDevicesIp30d,
        CountDeviceFingerprintsIp1d, CountDeviceFingerprintsIp7d, CountDeviceFingerprintsIp30d,
        CountCardsIp1d, CountCardsIp7d, CountCardsIp30d,
        CountPhonesIp1d, CountPhonesIp7d, CountPhonesIp30d,
        CountEmailsIp1d, CountEmailsIp7d, CountEmailsIp30d,
        CountUsersIp1d, CountUsersIp7d, CountUsersWallet30d,
        AverageSecondsBetweenTransactionsIp10m, AverageSecondsBetweenTransactionsIp30m,
        AverageSecondsBetweenTransactionsIp1h, AverageSecondsBetweenTransactionsIp1d,
        
        // Email counters (7)
        CountDevicesEmail1d, CountDevicesEmail7d, CountDevicesEmail30d,
        CountCardsEmail1d, CountCardsEmail7d, CountCardsEmail30d,
        CountSimilarEmails30d,
        
        // Card counters (13)
        CountEmailsCard1d, CountEmailsCard7d,
        CountIpCountriesCard1d, CountIpCountriesCard7d, CountIpCountriesCard30d,
        CountDevicesCard1d, CountDevicesCard7d, CountDevicesCard30d,
        CountUsersCard1d, CountUsersCard7d, CountUsersCard14d,
        CountSuccessDepUserCard30d, CountFailDepCard1d,
        
        // UserIP counters (6)
        CountUsersUserIp1d, CountUsersUserIp30d,
        AverageSecondsBetweenTransactionsUserIp10m, AverageSecondsBetweenTransactionsUserIp30m,
        AverageSecondsBetweenTransactionsUserIp1h, AverageSecondsBetweenTransactionsUserIp1d
    };

    /// <summary>
    /// Total number of counters.
    /// </summary>
    public static readonly int TotalCounterCount = AllCounterNames.Length;
}
