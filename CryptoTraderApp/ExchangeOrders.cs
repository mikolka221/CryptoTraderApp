
using System;

public class Rootobject
{
    public DateTime AcqTime { get; set; }
    public Bid[] Bids { get; set; }
    public Ask[] Asks { get; set; }
}

public class Bid
{
    public Order Order { get; set; }
}

public class Ask
{
    public Order Order { get; set; }
}

public class Order
{
    public object Id { get; set; }
    public DateTime Time { get; set; }
    public string Type { get; set; }
    public string Kind { get; set; }
    public float Amount { get; set; }
    public float Price { get; set; }
}