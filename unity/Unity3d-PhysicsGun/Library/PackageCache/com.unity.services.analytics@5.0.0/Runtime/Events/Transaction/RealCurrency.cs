using System;

namespace Unity.Services.Analytics
{
    public struct RealCurrency
    {
        /// <summary>
        /// The type of currency, for example GBP, USD, etc.
        /// </summary>
        public string RealCurrencyType;

        /// <summary>
        /// The amount of real currency, in the smallest unit applicable to that currency.
        /// Use <c>AnalyticsService.Instance.ConvertCurrencyToMinorUnits</c> to calculate currency if required.
        /// </summary>
        public Int64 RealCurrencyAmount;
    }
}
