namespace IEGEasyCreditcardService.Models
{
    public class CreditcardTransaction
    {
        public string CreditcardNumber { get; set; }
        public string CreditcardType { get; set; }
        public double Amount { get; set; }
        public string ReceiverName { get; set; }
    }
}
