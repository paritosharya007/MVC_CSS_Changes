using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Goalvisor.Common;
using Goalvisor.Helper;
using Goalvisor.Services.Subscriptions;
using Goalvisor.Services.Users;
using Goalvisor.ViewModels;
using Stripe;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Goalvisor.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly ISubscriptionsService _subscriptionService;
        private readonly IUserService _userService;

        public CheckoutController(ISubscriptionsService subscriptionService, IUserService userService)
        {
            _subscriptionService = subscriptionService;
            _userService = userService;
        }

        public async Task<IActionResult> SelectPackage(string subscriptionId)
        {
            var packages = await new StripeUtil().GetPackages();
            ViewBag.SubscriptionId = subscriptionId;
            return View(packages);
        }

        public IActionResult Checkout(string stripePackageId, string amount, string packageName, string priceId)
        {
            ViewBag.amount = amount;
            ViewBag.packageName = packageName;
            ViewBag.packageId = stripePackageId;
            //PriceId will be used for subscription
            ViewBag.priceId = priceId;
            return View();
        }

        public async Task<JsonResult> UpdatePackage(PaymentDetailsVM PaymentDetails)
        {
            PaymentDetails.StripePriceId = ScannerSuite3DES.Decrypt(PaymentDetails.StripePriceId);
            PaymentDetails.StripeSubId = ScannerSuite3DES.Decrypt(PaymentDetails.StripeSubId);
            PaymentDetails.StripePackageId = ScannerSuite3DES.Decrypt(PaymentDetails.StripePackageId);
            try
            {
                string msg = string.Empty;
                bool ok = false;

                StripeConfiguration.ApiKey = StripeUtil.SecretKey;

                //Get subscription detail from db if newPrice vs old prices id is same than show message
                //else update subscription
                var subDetail = await _subscriptionService.GetById(PaymentDetails.StripeSubId);
                if (subDetail != null)
                {
                    if (subDetail.StripePriceId.Equals(PaymentDetails.StripePriceId, StringComparison.OrdinalIgnoreCase))
                    {
                        return Json(new { ok = ok, msg = "Same package can't be updated." });
                    }
                    else
                    {
                        var service = new SubscriptionService();
                        var subscriptionResponse = service.Get(PaymentDetails.StripeSubId);

                        var options = new SubscriptionUpdateOptions
                        {
                            CancelAtPeriodEnd = false,
                            Items = new List<SubscriptionItemOptions>
                                    {
                                        new SubscriptionItemOptions
                                        {
                                            Id = subscriptionResponse.Items.Data[0].Id,
                                            Price = PaymentDetails.StripePriceId,
                                        }
                                    }
                        };
                        var updatedSubscription = service.Update(PaymentDetails.StripeSubId, options);
                        if (updatedSubscription.Status.ToLower().Equals("active"))
                        {
                            //update in local db
                            var productService = new ProductService();
                            Product packageInfo = productService.Get(PaymentDetails.StripePackageId);

                            var subscription = new Models.Subscription();
                            subscription.Name = packageInfo.Name;
                            subscription.Description = packageInfo.Description;
                            subscription.StripeProductId = PaymentDetails.StripePackageId;
                            subscription.StripePriceId = PaymentDetails.StripePriceId;
                            subscription.StripeSubId = subscriptionResponse.Id;
                            subscription.StripeCustomerId = subscriptionResponse.CustomerId;
                            subscription.StartDate = DateTime.Today;
                            subscription.Active = true;
                            subscription.EndDate = subscriptionResponse.CurrentPeriodEnd;

                            var response = await _subscriptionService.UpdateSubscription(subscription);
                            return Json(new { ok = response.Success, msg = response.Message });
                        }
                    }
                }
                return Json(new { ok = ok, msg = "No Subscription Found." });
            }
            catch (Exception d)
            {
                return Json(new { ok = false, msg = "Exception occured." });
            }
        }

        public async Task<JsonResult> ProcessPayment(PaymentDetailsVM PaymentDetails)
        {
            string msg = string.Empty;
            bool ok = false;
            StripeConfiguration.ApiKey = StripeUtil.SecretKey;

            PaymentDetails.StripePackageId = ScannerSuite3DES.Decrypt(PaymentDetails.StripePackageId);
            PaymentDetails.StripePriceId = ScannerSuite3DES.Decrypt(PaymentDetails.StripePriceId);
            PaymentDetails.Amount = ScannerSuite3DES.Decrypt(PaymentDetails.Amount);

            long Total = Convert.ToInt64(PaymentDetails.Amount);
            Customer customer = new Customer();
            try
            {
                decimal TempCharge = Convert.ToDecimal(Total);
                TempCharge = TempCharge * 100;
                Decimal charge = Math.Round(TempCharge);

                var cardoptions = new TokenCreateOptions
                {
                    Card = new CreditCardOptions
                    {
                        Number = PaymentDetails.CardNumber,
                        ExpYear = PaymentDetails.ExpirationYear,
                        ExpMonth = PaymentDetails.ExpirationMonth,
                        Cvc = PaymentDetails.CVV,
                    }
                };

                var tokenService = new TokenService();
                Token stripeToken = tokenService.Create(cardoptions);

                AddressOptions addressOptions = new AddressOptions();

                addressOptions.City = PaymentDetails.City;
                addressOptions.State = PaymentDetails.State;
                addressOptions.Country = PaymentDetails.Country;
                addressOptions.PostalCode = PaymentDetails.PostalCode;
                addressOptions.Line1 = PaymentDetails.Line1;
                addressOptions.Line2 = PaymentDetails.Line2;

                var cusoptions = new CustomerCreateOptions
                {
                    Description = "Online payment",
                    Name = PaymentDetails.NameOnCard,
                    Source = stripeToken.Id,
                    Address = addressOptions,
                };

                var cusservice = new CustomerService();
                customer = cusservice.Create(cusoptions);

                //Create payment method
                var paymentMethodoptions = new PaymentMethodListOptions
                {
                    Customer = customer.Id,
                    Type = "card",
                };

                //var service = new PaymentMethodService();
                var paymentMethService = new PaymentMethodService();
                StripeList<PaymentMethod> paymentMethods = paymentMethService.List(paymentMethodoptions
                );

                // Attach payment method
                var paymentOptions = new PaymentMethodAttachOptions
                {
                    Customer = customer.Id,
                };
                // var paymentMethService = new PaymentMethodService();
                var paymentMethod = paymentMethService.Attach(paymentMethods.Data[0].Id, paymentOptions);

                // Update customer's default invoice payment method
                var customerOptions = new CustomerUpdateOptions
                {
                    InvoiceSettings = new CustomerInvoiceSettingsOptions
                    {
                        DefaultPaymentMethod = paymentMethod.Id,
                    },
                };
                var customerService = new CustomerService();
                customerService.Update(customer.Id, customerOptions);

                // Create subscription
                var subscriptionOptions = new SubscriptionCreateOptions
                {
                    Customer = customer.Id,
                    Items = new List<SubscriptionItemOptions>
                        {
                            new SubscriptionItemOptions
                            {
                                Price = PaymentDetails.StripePriceId,
                            },
                        },
                };
                subscriptionOptions.AddExpand("latest_invoice.payment_intent");
                var subscriptionService = new SubscriptionService();
                try
                {
                    Stripe.Subscription subscriptionResponse = subscriptionService.Create(subscriptionOptions);
                    if (subscriptionResponse != null && !string.IsNullOrEmpty(subscriptionResponse.Id))
                    {
                        msg = "success";
                        ok = true;

                        if (!string.IsNullOrEmpty(PaymentDetails.StripePackageId))
                        {
                            var registeredUser = await _userService.GetByName(PaymentDetails.Username);
                            var productService = new ProductService();
                            Product packageInfo = productService.Get(PaymentDetails.StripePackageId);

                            var subscription = new Models.Subscription();

                            subscription.Name = packageInfo.Name;
                            subscription.Description = packageInfo.Description;
                            subscription.StripeProductId = PaymentDetails.StripePackageId;
                            subscription.StripePriceId = PaymentDetails.StripePriceId;
                            subscription.StripeSubId = subscriptionResponse.Id;
                            subscription.StripeCustomerId = subscriptionResponse.CustomerId;
                            subscription.StartDate = DateTime.Today;
                            subscription.EndDate = subscriptionResponse.CurrentPeriodEnd;

                            ServiceResult result = await _subscriptionService.CreateSubscription(registeredUser, subscription);//Create(registeredUser, null);
                            if (!result.Success)
                            {
                                var logAudit = new Models.Log();
                               // await _log.Add(new Models.Log() { Status = "Failed", Description = "Subscription failed", LogType = Models.LogTypeEnums.Subscription.ToString(), CreatedAt = DateTime.Now, OperationId = PaymentDetails.StripePackageId });
                            }
                        }
                    }
                    else
                    {
                        msg = "Failed to process payment";
                        ok = false;
                        var logAudit = new Models.Log();
                       // await _log.Add(new Models.Log() { Status = "Failed", Description = "Failed to process subscription!", LogType = Models.LogTypeEnums.Payment.ToString(), CreatedAt = DateTime.Now, OperationId = PaymentDetails.StripePackageId });
                    }
                    //return subscription;
                }
                catch (StripeException e)
                {
                    Console.WriteLine($"Failed to create subscription.{e}");
                    //return BadRequest();
                }
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, msg = ex.Message });
            }
            return Json(new { ok = ok, msg = msg });
        }
    }
}