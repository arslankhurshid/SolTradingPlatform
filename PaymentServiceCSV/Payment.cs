using CsvHelper.Configuration.Attributes;

namespace PaymentServiceCSV
{
    public class Payment
    {
        [Ignore] //wichtig, weil Id nicht beim POST übergeben wird. (CSV Problematik)
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string Receiver { get; set; }
    }
}
