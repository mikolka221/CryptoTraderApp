using System;
using System.Collections.Generic;
using System.Text;

namespace CryptoTraderApp
{
    public enum Fiat
    {
        EUR
    }

    public enum CryptoCurrency
    {
        BTC
    }

    public class ExchangeAccount
    {
        public string ExchangeName;
        public Dictionary<Fiat, double> fiatBalance { get; private set; } = new Dictionary<Fiat, double>();
        public Dictionary<CryptoCurrency, double> cryptoBalance { get; private set; } = new Dictionary<CryptoCurrency, double>();
        private Dictionary<Tuple<Fiat, CryptoCurrency>, OrderBook> orderBooks = new Dictionary<Tuple<Fiat, CryptoCurrency>, OrderBook>();

        public ExchangeAccount()
        {
            foreach (Fiat fiat in Enum.GetValues(typeof(Fiat)))
            {
                fiatBalance[fiat] = 0;
            }
            foreach (CryptoCurrency crypto in Enum.GetValues(typeof(CryptoCurrency)))
            {
                cryptoBalance[crypto] = 0;
            }
        }

        public ExchangeAccount(string name) : this()
        {
            ExchangeName = name;
        }

        public void SetFiatBalance(Fiat fiat, double newValue)
        {
            if (!(newValue < 0))
            {
                // Access dictionary with indexer: assign value or create new one if it doesn't exist.
                fiatBalance[fiat] = newValue;
            }
        }

        public void SetCryptoBalance(CryptoCurrency cryptoCurrency, double newValue)
        {
            if (!(newValue < 0))
            {
                // Access dictionary with indexer: assign value or create new one if it doesn't exist.
                cryptoBalance[cryptoCurrency] = newValue;
            }
        }

        public void ReadOrderBookFromFile(string fileName, Fiat fiat, CryptoCurrency cryptoCurrency)
        {
            var key = Tuple.Create(fiat, cryptoCurrency);
            if (!orderBooks.ContainsKey(key))
            {
                orderBooks.Add(key, new OrderBook(this, fiat, cryptoCurrency));
            }
            orderBooks[key].SetSourceDataFile(fileName);
        }

        public void UseBitstampWebSocket(Fiat fiat, CryptoCurrency cryptoCurrency)
        {
            var key = Tuple.Create(fiat, cryptoCurrency);
            if (!orderBooks.ContainsKey(key))
            {
                orderBooks.Add(key, new OrderBook(this, fiat, cryptoCurrency));
            }
            orderBooks[key].SetSourceBitstampWebSocket();
        }

        public void TurnOffBitstampWebSocket(Fiat fiat, CryptoCurrency cryptoCurrency)
        {
            var key = Tuple.Create(fiat, cryptoCurrency);
            if (orderBooks.ContainsKey(key))
            {
                orderBooks[key].TurnOffBitstampWebSocket();
            }
        }

        // Returns transactions for total price but not more than available balance of fiat
        public Transactions GetAsksForTotalPrice(double totalPrice, Fiat fiat, CryptoCurrency cryptoCurrency)
        {
            var result = orderBooks[Tuple.Create(fiat, cryptoCurrency)]?.GetAsksByTotalPrice(totalPrice) ?? null;
            if (result != null)
            {
                result.ReduceToTotalPrice(fiatBalance[fiat]);
            }
            return result;
        }

        // Returns transactions for totalAmount but not more than available balance for cryptoCurrency
        public Transactions GetBidsForTotalAmount(double totalAmount, Fiat fiat, CryptoCurrency cryptoCurrency)
        {
            var result = orderBooks[Tuple.Create(fiat, cryptoCurrency)]?.GetBidsByAmount(totalAmount) ?? null;
            if (result != null)
            {
                result.ReduceToAmount(cryptoBalance[cryptoCurrency]);
            }
            return result;
        }
    }
}
