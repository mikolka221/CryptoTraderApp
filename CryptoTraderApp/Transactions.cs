using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace CryptoTraderApp
{
    public class Transaction
    {
        public ExchangeAccount ExchangeAccount { get; set; }
        public double Price { get; set; }
        public double Amount { get; set; }
        public double GetTotalPrice() { return Price * Amount; }
        public void ReduceToTotalPrice(double newTotalPrice)
        {
            if (GetTotalPrice() > newTotalPrice)
            {
                Amount = newTotalPrice / Price;
            }
        }
        public Transaction(double price)
        {
            ExchangeAccount = null;
            Price = price;
        }
         public Transaction(double price, double amount) : this(price)
        {
            Amount = amount;
        }
        public Transaction(double price, double amount, ExchangeAccount exchangeAccount) : this(price, amount)
        {
            ExchangeAccount = exchangeAccount;
        }
    }

    public class Transactions
    {
        public enum Type
        {
            Bids,
            Asks
        }

        private sealed class TransactionComparer : IComparer<Transaction>
        {
            Type type;
            public TransactionComparer(Type type)
            {
                this.type = type;
            }

            public int Compare(Transaction x, Transaction y)
            {
                int result = 0;
                switch (type)
                {
                    case Type.Bids: // Sort descending
                        result = Math.Sign(y.Price - x.Price);
                        break;
                    case Type.Asks:
                        result = Math.Sign(x.Price - y.Price);
                        break;
                    default:
                        throw new Exception("Undefined comparer. Should be defined Ask/Bid.");
                }
                if (result == 0)
                {
                    // If values are the same, compare exchange by name.
                    // So transactions that have same price will be sorted by exchange name
                    result = x.ExchangeAccount?.ExchangeName.CompareTo(y.ExchangeAccount?.ExchangeName ?? "A") ?? -1;
                }
                return result;
            }
        }

        // Member variables
        private Type type;
        private TransactionComparer comparer;

        private List<Transaction> transactions = new List<Transaction>();
        public double totalPrice { get; private set; } = 0;
        public double totalAmount { get; private set; } = 0;

        // Transactions can only be instantiated by type
        public Transactions(Type type)
        {
            this.type = type;
            comparer = new TransactionComparer(type);
        }

        // Copy constructor
        public Transactions(Transactions other) : this(other.type)
        {
            this.transactions = new List<Transaction>(other.transactions);
            this.RecalculateTotalPriceAndAmount();
        }

        public double GetAvgPrice()
        {
            return totalPrice / totalAmount;
        }
        
        

        public override string ToString() 
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("\n********** Current transactions **********\n");
            sb.AppendFormat("Total Price = {0:N3}\n", totalPrice);
            sb.AppendFormat("Total Amount = {0:N3}\n", totalAmount);
            sb.AppendFormat("AVG price = {0:N3}\n", GetAvgPrice());
            sb.Append("********** List of transactions **********\n");
            int count = 0;
            foreach (Transaction transaction in transactions)
            {
                sb.AppendFormat("\t{0,2}\tTransaction price = {1,10:N2}, transaction amount = {2,6:N2}, Total price = {3,10:N2}, Exchange = {4}\n",
                    ++count, transaction.Price, transaction.Amount, transaction.GetTotalPrice(), transaction.ExchangeAccount?.ExchangeName??"<>");
            }
            sb.Append("********** End of transactions **********\n");
            return sb.ToString();
        }

        public void AddTransaction(double price, double amount, ExchangeAccount exchangeAccount = null)
        {
            int index = IndexOf(price);
            if (index < 0)
            {
                // Add new item to -index
                transactions.Insert(~index, new Transaction(price, amount, exchangeAccount));
                totalAmount += amount;
                totalPrice += price * amount;
            }
            else
            {
                if (0 == amount)
                {
                    totalAmount -= transactions[index].Amount;
                    totalPrice -= transactions[index].Price * transactions[index].Amount;
                    transactions.RemoveAt(index);
                }
                else
                {
                    double amountDiff = amount - transactions[index].Amount;
                    transactions[index].Amount = amount;
                    totalAmount += amountDiff;
                    totalPrice += transactions[index].Price * amountDiff;
                }
            }
            
        }

        public void AddTransactions(Transactions other)
        {
            transactions.AddRange(other.transactions);
            transactions.Sort(comparer);
            this.totalAmount += other.totalAmount;
            this.totalPrice += other.totalPrice;
        }

        public void ReduceToAmount(double amount)
        {
            double diff = 0;
            while ((diff = totalAmount - amount) > 0)
            {
                int lastIndex = transactions.Count - 1;
                double lastTransactionAmount = transactions[lastIndex].Amount;
                if (lastTransactionAmount > diff)
                {
                    transactions[lastIndex].Amount = lastTransactionAmount - diff;
                    // Calculate precise totalAmount and totalPrice - Error can be acumulated
                    RecalculateTotalPriceAndAmount();
                }
                else
                {
                    totalAmount -= lastTransactionAmount;
                    totalPrice -= transactions[lastIndex].GetTotalPrice();
                    transactions.RemoveAt(lastIndex);
                }
            }
        }

        public void ReduceToTotalPrice(double price)
        {
            double diff = 0;
            while ((diff = totalPrice - price) > 0)
            {
                int lastIndex = transactions.Count - 1;
                double lastTransactionTotalPrice = transactions[lastIndex].GetTotalPrice();
                if (lastTransactionTotalPrice > diff)
                {
                    transactions[lastIndex].ReduceToTotalPrice(lastTransactionTotalPrice - diff);
                    // Calculate precise totalAmount and totalPrice - Error can be acumulated
                    RecalculateTotalPriceAndAmount();
                }
                else
                {
                    totalAmount -= transactions[lastIndex].Amount;
                    totalPrice -= lastTransactionTotalPrice;
                    transactions.RemoveAt(lastIndex);
                }
            }
        }

        // Iterate over all transactions and calculate precise amount and value
        private void RecalculateTotalPriceAndAmount()
        {
            totalAmount = 0;
            totalPrice = 0;
            foreach (var tr in transactions)
            {
                totalAmount += tr.Amount;
                totalPrice += tr.Amount * tr.Price;
            }
        }

        public void SetAllExchangeAccount(ExchangeAccount exchangeAccount)
        {
            foreach (Transaction transaction in transactions)
            {
                transaction.ExchangeAccount = exchangeAccount;
            }
        }

        /**********************************************************/
        // Binary search
        public int IndexOf(double price)
        {
            return transactions.BinarySearch(new Transaction(price), comparer);
        }

        public void Clear()
        {
            transactions.Clear();
            totalAmount = 0;
            totalPrice = 0;
        }

        public bool Contains(double price)
        {
            return IndexOf(price) >= 0;
        }
    }
}
