namespace Goalvisor.ViewModels
{
    public class PaymentDetailsVM
    {
        public string CardNumber { get; set; }
        public long ExpirationMonth { get; set; }
        public long ExpirationYear { get; set; }
        public string NameOnCard { get; set; }
        public string Brand { get; set; }
        public string CVV { get; set; }

        //public int PackageId { get; set; }
        /// <summary>
        /// This is product Id
        /// </summary>
        public string StripePackageId { get; set; }

        public string StripePriceId { get; set; }
        public string StripeSubId { get; set; }
        public string Amount { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string PostalCode { get; set; }
        public string Line1 { get; set; }
        public string Line2 { get; set; }
        public string Username { get; set; }
    }
}