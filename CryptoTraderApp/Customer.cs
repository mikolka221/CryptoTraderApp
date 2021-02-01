using System;
using System.Collections.Generic;
using System.Text;

namespace CryptoTraderApp
{
    class Customer
    {
        public List<ExchangeAccount> exchangeAccounts { get; private set; } =
            new List<ExchangeAccount>();

        public Customer()
        {

        }

        public ExchangeAccount AddExchangeAccount(string exchangeName)
        {
            if (exchangeAccounts.Exists((x) => { return 0 == String.Compare(x.ExchangeName, exchangeName, true); }))
            {
                return null;
            }
            ExchangeAccount result = new ExchangeAccount(exchangeName);
            exchangeAccounts.Add(result);
            return result;
        }

        public ExchangeAccount GetExchangeAccount(string exchangeName)
        {
            return exchangeAccounts.Find((x) => { return 0 == String.Compare(x.ExchangeName, exchangeName, true); });
        }

        public Transactions BuyCrypto(double amount, Fiat fiat, CryptoCurrency crypto)
        {
            Transactions result = new Transactions(Transactions.Type.Asks);
            foreach (var exchangeAccount in exchangeAccounts)
            {
                result.AddTransactions(exchangeAccount.GetAsksForTotalPrice(amount, fiat, crypto));
            }
            result.ReduceToTotalPrice(amount);

            return result;
        }

        public Transactions SellCrypto(double amount, Fiat fiat, CryptoCurrency crypto)
        {
            Transactions result = new Transactions(Transactions.Type.Bids);
            foreach (var exchangeAccount in exchangeAccounts)
            {
                result.AddTransactions(exchangeAccount.GetBidsForTotalAmount(amount, fiat, crypto));
            }
            result.ReduceToAmount(amount);

            return result;
        }

        
    }
}
