using Goalvisor.Models;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Goalvisor.Helper
{
    public class StripeUtil
    {
        //Original Key
        public static string SecretKey = "sk_test_51HdgwbL3fnQJAUkc4HuS5iAmBYjXTzaYSrt2STrIFLjUEbWe7AaZCnoGba6YebRkCcL5pfyQp2rLJOCIfhqOIVEz00b7R37mlC";

        //Dev Key
        //

        public async Task<List<Package>> GetPackages()
        {
            StripeConfiguration.ApiKey = SecretKey;

            //Get all product list
            var options = new ProductListOptions
            {
                Limit = 100,
                Active = true
            };
            var productService = new ProductService();
            StripeList<Product> products = await productService.ListAsync(options);

            //Get all Prices
            var priceOptions = new PriceListOptions { Limit = 100 };
            var priceService = new PriceService();
            StripeList<Price> prices = priceService.List(priceOptions);

            var listOfPackages = new List<Package>();
            foreach (var product in products.Data)
            {
                var priceOfProduct = prices.FirstOrDefault(x => x.ProductId == product.Id);
                listOfPackages.Add(new Package
                {
                    Id = 1,
                    ProductId = product.Id,
                    StripePriceId = priceOfProduct.Id,
                    Name = product.Name,
                    Description = product.Description,
                    Price = (double)(priceOfProduct.UnitAmount / 100),
                    Interval = priceOfProduct.Recurring.Interval
                });
            }
            return listOfPackages.OrderBy(x => x.Price).ToList();
        }

        public async Task<string> GetSubscriptionsByCustomerId(string stripeCustomerId)
        {
            StripeConfiguration.ApiKey = StripeUtil.SecretKey;
            try
            {
                // Check User payment detail if he/she has not paid restrict him/her for login
                var options = new SubscriptionListOptions
                {
                    Customer = "cus_Ii6zFDbvE4cNgy",
                    Limit = 100,
                    Status = "active"
                };
                //customer = 3,//
                var service = new SubscriptionService();
                StripeList<Stripe.Subscription> subscriptions = await service.ListAsync(
                  options
                );
                foreach (var subscription in subscriptions)
                {
                    //If any of subscription amount is pending show message to user
                    var currentDate = DateTime.Now;
                    var res = DateTime.Compare(currentDate, subscription.CurrentPeriodEnd);
                    if (res > 0)
                        return "Sorry, your subscription is expired.";
                    else
                        return "allow";
                }
            }
            catch (Exception ex)
            {
                return "error";
            }
            return "fail";
        }
    }
}