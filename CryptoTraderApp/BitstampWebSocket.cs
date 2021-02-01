using System;
using System.Collections.Generic;
using System.Text;

namespace CryptoTraderApp
{
    class BitstampWebSocket : IOrderBookDataProvider
    {
        public class JsonData
        {
            public Data data { get; set; }
            public string channel { get; set; }
            public string _event { get; set; }
        }

        public class Data
        {
            public string timestamp { get; set; }
            public string microtimestamp { get; set; }
            public string[][] bids { get; set; }
            public string[][] asks { get; set; }
        }

        private Fiat fiat;
        private CryptoCurrency cryptoCurrency;
        private string currencyPair;
        private System.Threading.Thread thread;
        private Transactions Bids = new Transactions(Transactions.Type.Bids);
        private Transactions Asks = new Transactions(Transactions.Type.Asks);
        private ExchangeAccount _exchangeAccount = null;
        public ExchangeAccount exchangeAccount { get { return _exchangeAccount; } }
        volatile bool readData = false;
        System.Net.WebSockets.ClientWebSocket cws;

        System.Threading.CancellationToken token = new System.Threading.CancellationToken();
        bool read = true;

        public BitstampWebSocket(ExchangeAccount exchangeAccount, Fiat fiat, CryptoCurrency cryptoCurrency)
        {
            _exchangeAccount = exchangeAccount;
            this.fiat = fiat;
            this.cryptoCurrency = cryptoCurrency;
            currencyPair = String.Format("{0}{1}", cryptoCurrency.ToString().ToLower(), fiat.ToString().ToLower());
        }

        ~BitstampWebSocket()
        {
            this.StopReadingData();
        }

        public void StartReadingData()
        {
            cws = new System.Net.WebSockets.ClientWebSocket();
            token.Register(OnCancel);
            cws.ConnectAsync(new Uri("wss://ws.bitstamp.net"), token).Wait();
            string msgSubscribe = String.Format("{{\"event\": \"bts:subscribe\",\"data\": {{\"channel\": \"order_book_{0}\"}}}}\r\n", currencyPair);
            string msgUnubscribe = String.Format("{{\"event\": \"bts:unsubscribe\",\"data\": {{\"channel\": \"order_book_{0}\"}}}}\r\n", currencyPair);
            cws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(msgSubscribe)), System.Net.WebSockets.WebSocketMessageType.Binary, true, token);
            ReadMsgSync(token);
            string msg = ReadMsgSync(token);
            cws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(msgUnubscribe)), System.Net.WebSockets.WebSocketMessageType.Text, true, token);
            JsonData data = System.Text.Json.JsonSerializer.Deserialize<JsonData>(msg);
            Bids.Clear();
            Asks.Clear();
            MoveJsonDataToTransactions(data);
            thread = new System.Threading.Thread(ReadAndUpdateData);
            thread.Start();
        }

        private void ReadAndUpdateData()
        {
            // Works better if new data is allways loaded
            bool update = false;
            if (update)
            {
                string msgSubscribeDiff = String.Format("{{\"event\": \"bts:subscribe\",\"data\": {{\"channel\": \"diff_order_book_{0}\"}}}}\r\n", currencyPair);
                string msgUnubscribeDiff = String.Format("{{\"event\": \"bts:unsubscribe\",\"data\": {{\"channel\": \"diff_order_book_{0}\"}}}}\r\n", currencyPair);

                readData = true;
                cws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(msgSubscribeDiff)), System.Net.WebSockets.WebSocketMessageType.Binary, true, token);
                while (readData)
                {
                    string msg = ReadMsgSync(token);
                    JsonData data = System.Text.Json.JsonSerializer.Deserialize<JsonData>(msg);
                    MoveJsonDataToTransactions(data);
                    //Console.WriteLine(msg);
                }
                cws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(msgUnubscribeDiff)), System.Net.WebSockets.WebSocketMessageType.Text, true, token);
            }
            else
            {
                string msgSubscribe = String.Format("{{\"event\": \"bts:subscribe\",\"data\": {{\"channel\": \"order_book_{0}\"}}}}\r\n", currencyPair);
                string msgUnubscribe = String.Format("{{\"event\": \"bts:unsubscribe\",\"data\": {{\"channel\": \"order_book_{0}\"}}}}\r\n", currencyPair);

                readData = true;
                cws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(msgSubscribe)), System.Net.WebSockets.WebSocketMessageType.Binary, true, token);
                string reply = ReadMsgSync(token);
                while (readData)
                {
                    string msg = ReadMsgSync(token);
                    JsonData data = System.Text.Json.JsonSerializer.Deserialize<JsonData>(msg);
                    if (((data?.data ?? null) != null) && (data.data.asks != null) && (data.data.bids != null))
                    {
                        MoveJsonDataToTransactions(data, false);
                    }
                }
                cws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(msgUnubscribe)), System.Net.WebSockets.WebSocketMessageType.Text, true, token);
            }
            cws.CloseAsync(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, "", token);

        }

        public void StopReadingData()
        {
            readData = false;
        }

        public void MoveJsonDataToTransactions(JsonData data, bool update = true)
        {
            if ((data.data?.bids ?? null) != null)
            {
                lock (Bids)
                {
                    if (!update) Bids.Clear();
                    foreach (var bid in data.data.bids)
                    {
                        double price, amount;
                        if (Double.TryParse(bid[0], System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out price) &&
                            Double.TryParse(bid[1], System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out amount))
                        {
                            Bids.AddTransaction(price, amount, exchangeAccount);
                        }
                    }
                }
            }
            if ((data.data?.asks ?? null) != null)
            {
                lock (Asks)
                {
                    if (!update) Asks.Clear();
                    foreach (var ask in data.data.asks)
                    {
                        double price, amount;
                        if (Double.TryParse(ask[0], System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out price) &&
                            Double.TryParse(ask[1], System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out amount))
                        {
                            Asks.AddTransaction(price, amount, exchangeAccount);
                        }
                    }
                }
            }
        }

        public async System.Threading.Tasks.Task<string> ReadMsg(System.Threading.CancellationToken token)
        {
            System.Net.WebSockets.WebSocketReceiveResult res;
            ArraySegment<byte> buf = new ArraySegment<byte>();
            res = await cws.ReceiveAsync(buf, token);
            return buf.ToString();
        }
        public string ReadMsgSync(System.Threading.CancellationToken token)
        {
            //System.Net.WebSockets.WebSocketReceiveResult res;
            StringBuilder response = new StringBuilder(10000);
            ArraySegment<byte> buf = new ArraySegment<byte>(new byte[2000]);
            System.Threading.Tasks.Task<System.Net.WebSockets.WebSocketReceiveResult> res;
            bool reading = true;
            while (reading)
            {
                res = cws.ReceiveAsync(buf, token);
                res.Wait();
                if (res.IsCompletedSuccessfully)
                {
                    response.Append(System.Text.Encoding.Default.GetString(buf), 0, res.Result.Count);
                    reading = !res.Result.EndOfMessage;
                }
                else
                {
                    reading = false;
                }
            }
            string result = response.ToString();
            if (false && (response.Length > 2000))
            {
                System.IO.File.WriteAllText(@"C:\Users\Nikola\Desktop\SowaLabsNaloga\Data\BitstampData.json", result);
                read = false;
            }
            return result;
        }

        public void OnCancel()
        {
            int a = 0;
        }

        public Transactions GetBidsByAmount(double amount)
        {
            Transactions result;
            lock (Bids)
            {
                result = new Transactions(Bids);
            }
            result.ReduceToAmount(amount);
            return result;
        }

        public Transactions GetAsksByTotalPrice(double totalPrice)
        {
            Transactions result;
            lock (Asks)
            {
                result = new Transactions(Asks);
            }
            result.ReduceToTotalPrice(totalPrice);
            return result;
        }
    }
}
