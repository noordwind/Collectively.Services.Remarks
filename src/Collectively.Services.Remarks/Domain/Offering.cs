using System;
using Collectively.Common.Domain;
using Collectively.Common.Extensions;

namespace Collectively.Services.Remarks.Domain
{
    public class Offering : ValueObject<Offering>
    {
        public decimal Price { get; protected set; }
        public string Currency { get; protected set; }
        public DateTime? StartDate { get; protected set; }
        public DateTime? EndDate { get; protected set; }

        public static class Currencies
        {
            public static string PLN => "PLN";
            public static string EUR => "EUR";
            public static string USD => "USD";
            public static string BTC => "BTC";

            public static string Parse(string currency)
            {
                if (currency.Empty())
                {
                    throw new ArgumentException("Currency can not be empty.", nameof(currency));
                }
                switch (currency.ToLowerInvariant())
                {
                    case "pln": return PLN;
                    case "eur": return EUR;
                    case "usd": return USD;
                    case "btc": return BTC;
                }
                throw new ArgumentException("Invalid currency.", nameof(currency));
            }
        }

        protected Offering()
        {
        }

        protected Offering(decimal price, string currency,
            DateTime? startDate, DateTime? endDate)
        {
            if (price <= 0)
            {
                throw new ArgumentException("Offering price must be greater than 0.", nameof(price));
            }
            Price = price;
            Currency = Currencies.Parse(currency);
            StartDate = startDate;
            EndDate = endDate;
        }

        public static Offering Create(decimal price, string currency,
            DateTime? startDate, DateTime? endDate)
            => new Offering(price, currency, startDate, endDate);

        protected override bool EqualsCore(Offering other)
            => Price.Equals(other.Price) && Currency.Equals(other.Currency) &&
                StartDate.Equals(other.StartDate) && EndDate.Equals(other.EndDate);

        protected override int GetHashCodeCore()
        {
            var hash = 13;
            hash = (hash * 7) + Price.GetHashCode();
            hash = (hash * 7) + Currency.GetHashCode();
            hash = (hash * 7) + StartDate.GetHashCode();
            hash = (hash * 7) + EndDate.GetHashCode();

            return hash;
        }
    }
}