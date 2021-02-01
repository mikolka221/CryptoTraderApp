using System;
using System.Collections.Generic;
using System.Text;

namespace CryptoTraderApp
{
    class OrderBookFileReader : IOrderBookDataProvider
    {
        private OrderBookData orderBookData = null;
        private ExchangeAccount _exchangeAccount = null;
        public ExchangeAccount exchangeAccount { get { return _exchangeAccount; } }

        public OrderBookFileReader(ExchangeAccount exchangeAccount)
        {
            _exchangeAccount = exchangeAccount;
        }

        public void ReadOrderBookFile(string fileName)
        {
            string allText = System.IO.File.ReadAllText(fileName);
            int index = allText.IndexOf('{');
            orderBookData = System.Text.Json.JsonSerializer.Deserialize<OrderBookData>(allText.Substring(index));
        }

        public Transactions GetAsksByTotalPrice(double totalPrice)
        {
            Transactions result = new Transactions(Transactions.Type.Asks);
            // Total with all transactions from list
            if (orderBookData != null)
            {
                foreach (var ask in orderBookData.Asks)
                {
                    result.AddTransaction(ask.Order.Price, ask.Order.Amount);
                }
                result.ReduceToTotalPrice(totalPrice);
            }
            return result;
        }

        public Transactions GetBidsByAmount(double amount)
        {
            Transactions result = new Transactions(Transactions.Type.Bids);
            // Total with all transactions from list
            if (orderBookData != null)
            {
                foreach (var bid in orderBookData.Bids)
                {
                    result.AddTransaction(bid.Order.Price, bid.Order.Amount, exchangeAccount);
                }
                result.ReduceToAmount(amount);
            }
            return result;
        }

        // Internal data structure for reading data from json
        private class OrderBookData
        {
            public DateTime AcqTime { get; set; }
            public BidAskList[] Bids { get; set; }
            public BidAskList[] Asks { get; set; }
        }

        private class BidAskList
        {
            public Order Order { get; set; }
        }

        private sealed class Order
        {
            public object Id { get; set; }
            public DateTime Time { get; set; }
            public string Type { get; set; }
            public string Kind { get; set; }
            public double Amount { get; set; }
            public double Price { get; set; }
        }
    }
}
