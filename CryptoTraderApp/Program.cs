using System;

namespace CryptoTraderApp
{
    class Program
    {
        static public string DataPath = System.IO.Path.GetFullPath( System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\Data\\"));
        static void PrintHelp()
        {
            Console.WriteLine("\n********** HELP ********** \n");
            Console.WriteLine("\n\"H\" or \"?\" - display help.");
            Console.WriteLine("\n\"C\" - Buy crypto mode.");
            Console.WriteLine("\n\"F\" - Buy fiat mode.");
            Console.WriteLine("\n\"X\" - Exit.");
            Console.WriteLine("\n********** HELP ********** \n");
        }

        static void Main(string[] args)
        {
            Customer customer = new Customer();
            int demoMode = 2;
            Console.WriteLine("\nSelect demo mode: 1- Read from data files, 2- Read from Bitstamp.");
            string mode = Console.ReadLine();
            int.TryParse(mode, out demoMode);
                bool buyCryptoMode = true;
            if (demoMode == 2)
            {
                var bitstamp = customer.AddExchangeAccount("Bitstamp");
                bitstamp.SetFiatBalance(Fiat.EUR, 100000);
                bitstamp.SetCryptoBalance(CryptoCurrency.BTC, 10);
                bitstamp.UseBitstampWebSocket(Fiat.EUR, CryptoCurrency.BTC);
                Console.WriteLine("\nDEMO MODE 2 => Live data from Bitstamp, you have {0} EUR and {1} BTC.",
                    bitstamp.fiatBalance[Fiat.EUR], bitstamp.cryptoBalance[CryptoCurrency.BTC]);
            }
            else if (demoMode == 1)
            {
                Console.WriteLine("\nDEMO MODE 1 => Data from files, each exchange has 5000 EUR and 1 BTC.");

                var exch1 = customer.AddExchangeAccount("Exchange1");
                exch1.ReadOrderBookFromFile(DataPath + "EURBTCexchange1.txt",
                    Fiat.EUR, CryptoCurrency.BTC);
                exch1.SetFiatBalance(Fiat.EUR, 5000.0);
                exch1.SetCryptoBalance(CryptoCurrency.BTC, 1.0);

                var exch2 = customer.AddExchangeAccount("Exchange2");
                exch2.ReadOrderBookFromFile(DataPath + "EURBTCexchange2.txt",
                    Fiat.EUR, CryptoCurrency.BTC);
                exch2.SetFiatBalance(Fiat.EUR, 5000.0);
                exch2.SetCryptoBalance(CryptoCurrency.BTC, 1.0);

                var exch3 = customer.AddExchangeAccount("Exchange3");
                exch3.ReadOrderBookFromFile(DataPath + "EURBTCexchange3.txt",
                    Fiat.EUR, CryptoCurrency.BTC);
                exch3.SetFiatBalance(Fiat.EUR, 5000.0);
                exch3.SetCryptoBalance(CryptoCurrency.BTC, 1.0);

                var exch4 = customer.AddExchangeAccount("Exchange4");
                exch4.ReadOrderBookFromFile(DataPath + "EURBTCexchange4.txt",
                    Fiat.EUR, CryptoCurrency.BTC);
                exch4.SetFiatBalance(Fiat.EUR, 5000.0);
                exch4.SetCryptoBalance(CryptoCurrency.BTC, 1.0);
            }

            // Main loop
            Console.WriteLine("\nType \"H\" or \"?\" for help.");
            while (true)
            {
                if (buyCryptoMode)
                {
                    Console.WriteLine("Buy crypto mode: How many EUR would you like to change for BTC?\n");
                }
                else
                {
                    Console.WriteLine("Buy fiat mode: How many BTC would you like to change for EUR?\n");
                }
                string res;
                res = Console.ReadLine();
                if ((0 == String.Compare(res, "H", true)) || (0 == String.Compare(res, "?", true)))
                {
                    PrintHelp();
                }
                else if (0 == String.Compare(res, "C", true))
                {
                    buyCryptoMode = true;
                }
                else if (0 == String.Compare(res, "F", true))
                {
                    buyCryptoMode = false;
                }
                else if (0 == String.Compare(res, "X", true))
                {
                    Console.WriteLine("\nGoodbye!");
                    Console.Read();
                    break;
                }
                else
                {
                    double value;
                    if (Double.TryParse(res, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out value))
                    {
                        Transactions bitstampOrder;
                        if (buyCryptoMode)
                        {
                            bitstampOrder = customer.BuyCrypto(value, Fiat.EUR, CryptoCurrency.BTC);
                        }
                        else
                        {
                            bitstampOrder = customer.SellCrypto(value, Fiat.EUR, CryptoCurrency.BTC);
                        }
                        //var asks = bitstamp.GetAsksForTotalPrice(value, Fiat.EUR, CryptoCurrency.BTC);
                        Console.WriteLine(bitstampOrder.ToString());
                    }
                    else
                    {
                        Console.WriteLine("Wrong input.\n");
                    }
                }
            }

            if (demoMode == 2)
            {
                customer.GetExchangeAccount("Bitstamp").TurnOffBitstampWebSocket(Fiat.EUR, CryptoCurrency.BTC);
            }
        }
    }
}
