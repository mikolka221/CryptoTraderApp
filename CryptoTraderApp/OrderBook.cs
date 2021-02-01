using System;
using System.Collections.Generic;
using System.Text;

namespace CryptoTraderApp
{
    interface IOrderBookDataProvider/* : IDisposable*/ //should be
    {
        // Parent exchange account
        ExchangeAccount exchangeAccount { get; }

        // Returns transactions required to get amount
        Transactions GetBidsByAmount(double amount);

        // Returns transactions required to spend totalPrice
        Transactions GetAsksByTotalPrice(double totalPrice);
    }

    class OrderBook
    {
        // Name of order book eg: EUR/BTC
        public string name;
        private Fiat fiat;
        private CryptoCurrency cryptoCurrency;
        IOrderBookDataProvider dataProvider;
        private ExchangeAccount exchangeAccount;    // Parent exchange account

        public OrderBook(ExchangeAccount exchangeAccount, Fiat fiat, CryptoCurrency cryptoCurrency)
        {
            this.exchangeAccount = exchangeAccount;
            this.fiat = fiat;
            this.cryptoCurrency = cryptoCurrency;
            name = String.Format("{0}-{1}", fiat.ToString(), cryptoCurrency.ToString());
        }

        public void SetSourceDataFile(string fileName)
        {
            var tempFileReader = new OrderBookFileReader(exchangeAccount);
            tempFileReader.ReadOrderBookFile(fileName);
            dataProvider = tempFileReader;
        }

        public void SetSourceBitstampWebSocket()
        {
            var webSocket = new BitstampWebSocket(exchangeAccount, fiat, cryptoCurrency);
            webSocket.StartReadingData();
            dataProvider = webSocket;
        }

        public void TurnOffBitstampWebSocket()
        {
            var webSocket = dataProvider as BitstampWebSocket;
            webSocket.StopReadingData();
        }

        // Returns transactions required to get amount
        public Transactions GetBidsByAmount(double amount)
        {
            var result = dataProvider.GetBidsByAmount(amount);
            result?.SetAllExchangeAccount(exchangeAccount);
            return result;
        }

        // Returns transactions required to spend totalPrice
        public Transactions GetAsksByTotalPrice(double totalPrice)
        {
            var result = dataProvider.GetAsksByTotalPrice(totalPrice);
            result?.SetAllExchangeAccount(exchangeAccount);
            return result;
        }
    }
}
