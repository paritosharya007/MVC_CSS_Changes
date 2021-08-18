using Goalvisor.Common;
using Goalvisor.Data;
using Goalvisor.Helper;
using Goalvisor.Models;
using Goalvisor.Services.Affiliate;
using Goalvisor.Services.Email;
using Goalvisor.Services.Subscriptions;
using Goalvisor.Services.Users;
using Goalvisor.ViewModels;
using Goalvisor.ViewModels.Authentication;
using Goalvisor.ViewModels.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Goalvisor.Controllers
{
    public class AccountController : Controller
    {
        private IHostingEnvironment _hostEnvironment;
        private readonly IEmailService _emailService;

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _dbContext;
        private readonly IUserService _userService;
        private readonly ISubscriptionsService _subscriptionService;
        private readonly IAffiliateService _affiliateService;

        private readonly IConfiguration _configuration;
        private IHttpContextAccessor _contextAccessor;

        public bool DisplayConfirmAccountLink { get; set; }
        public string EmailConfirmationUrl { get; set; }
        public string baseUrl { get; set; }

        public AccountController(UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext dbContext,
            IHttpContextAccessor contextAccessor,
            IUserService userService,
            ISubscriptionsService subscriptionService,
            IConfiguration configuration, IEmailService emailService, IHostingEnvironment hostEnvironment, IAffiliateService affiliateService
            )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _dbContext = dbContext;
            _contextAccessor = contextAccessor;
            _userService = userService;
            _subscriptionService = subscriptionService;
            _configuration = configuration;
            _emailService = emailService;
            _hostEnvironment = hostEnvironment;
            _affiliateService = affiliateService;
        }

        [HttpGet]
        public IActionResult Register(string returnUrl = null, string stripePackageId = "", string stripePriceId = "", string r = "")
        {
            ViewData["ReturnUrl"] = returnUrl;

            baseUrl = PathHelper.FullyQualifiedApplicationPath(ControllerContext.HttpContext.Request);

            var model = new RegistrationModel();
            model.StripePackageId = ScannerSuite3DES.Decrypt(stripePackageId);
            model.StripePriceId = ScannerSuite3DES.Decrypt(stripePriceId);

            string refferalLink = baseUrl + "join?r=" + r;

            var refferedByUserId = _affiliateService.GetByLink(refferalLink);

            if (refferedByUserId != null)
            {
                if (refferedByUserId.Id > 0)
                {
                    model.ReferralCode = refferedByUserId.Id;
                }
            }
            return View(model);
        }

        #region "Generate Email Template"

        private EmailTemplate RegisterEmailTemplate(RegistrationModel registrationModel)
        {
            string body = string.Empty;
            string path = Path.Combine(this._hostEnvironment.WebRootPath, "template.html");
            using (StreamReader reader = new StreamReader(path))
            {
                body = reader.ReadToEnd();
            }

            var result = _emailService.GetEmailBody("Registration").Result;

            string emailbody = result.Body.Replace("{recipient}", registrationModel.UserName).Replace("{fullname}", registrationModel.Name).Replace("{activationlink}", "");
            ///dont change below line spaces
            string[] _bodyText = emailbody.Split(@"");
            string _body = "";
            string _signText = "";
            for (int i = 0; i < _bodyText.Length; i++)
            {
                if (i == 0) /// Greetings line (Dear {forename})
                {
                    _body += "<span><h2>" + _bodyText[i] + "</h2></span>";
                }
                else
                {
                    if (i == _bodyText.Length - 2)  /// Signature first line and body lastline
                    {
                        var firstLine = _bodyText[i].Split('\n').FirstOrDefault();
                        var lastLine = _bodyText[i].Split('\n').LastOrDefault();
                        _body += firstLine;
                        _signText += lastLine + "<br />";
                    }
                    else if (i == _bodyText.Length - 1) /// Signature last line
                    {
                        _signText += _bodyText[i];
                    }
                    else
                    {
                        _body += _bodyText[i];
                    }
                }
            }
            body = body.Replace("{body}", _body);
            body = body.Replace("{activationlink}", EmailConfirmationUrl);
            body = body.Replace("{signature}", _signText);

            var _template = new EmailTemplate();
            _template.Subject = result.Subject;
            _template.Body = body;

            return _template;
        }

        private EmailTemplate ResetEmailTemplate(string email, string resetUrl)
        {
            string body = string.Empty;
            string path = Path.Combine(this._hostEnvironment.WebRootPath, "resetpasswordtemplate.html");
            using (StreamReader reader = new StreamReader(path))
            {
                body = reader.ReadToEnd();
            }

            var result = _emailService.GetEmailBody("Password").Result;

            string emailbody = result.Body.Replace("{recipient}", email).Replace("{fullname}", email).Replace("{activationlink}", "");
            ///dont change below line spaces
            string[] _bodyText = emailbody.Split(@"");
            string _body = "";
            string _signText = "";
            for (int i = 0; i < _bodyText.Length; i++)
            {
                if (i == 0)  /// this condition for greetings line (ex. Dear X)
                {
                    _body += "<span><h2>" + _bodyText[i] + "</h2></span>";
                }
                else
                {
                    if (i == _bodyText.Length - 2)  /// this condition for signature firstline (ex. Kind regards) and body lastline
                    {
                        var firstLine = _bodyText[i].Split('\n').First();
                        var lastLine = _bodyText[i].Split('\n').Last();
                        _body += firstLine;
                        _signText += lastLine + "<br />";
                    }
                    else if (i == _bodyText.Length - 1)   /// this condition for signature lastline
                    {
                        _signText += _bodyText[i];
                    }
                    else
                    {
                        _body += _bodyText[i];
                    }
                }
            }

            body = body.Replace("{resetlink}", resetUrl);
            body = body.Replace("{signature}", _signText);
            var _template = new EmailTemplate();
            _template.Subject = result.Subject;
            _template.Body = body;
            return _template;
        }

        #endregion "Generate Email Template"

        [HttpPost]
        public async Task<IActionResult> Register(RegistrationModel Input, bool apiRequest = false, string returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                int referredByUserId = 0;
                int referralId = 0;
                // TODO: Is RefferalCode still needed?
                if (Input.ReferralCode > 0)
                {
                    var referbyId = await _affiliateService.GetById(Input.ReferralCode);
                    if (referbyId != null)
                    {
                        referredByUserId = referbyId.UserId;
                    }
                    referralId = Input.ReferralCode;
                }
                var user = new UserVM { Email = Input.Email, FullName = Input.Name, Password = Input.Password, UserName = Input.UserName, ReferralCode = referralId, ReferredBy = referredByUserId };

                var result = await _userService.Add(user);
                if (result.Success)
                {
                    // First user created will be automatically assigned the role Administrator
                    var registeredUser = await _userService.GetByName(user.UserName);
                    if (await _dbContext.Users.CountAsync() == 1)
                    {
                        // TODO: Create SetUserAsAdmin and remove this section
                        var addAdminRole = await _userService.UpdateRoles(registeredUser.Id, new List<string> { RunTimeElements.AdministratorRole, RunTimeElements.UserRole });
                    }

                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(registeredUser);
                    EmailConfirmationUrl = Url.Action("ConfirmEmail", "Account", new { token, email = registeredUser.Email }, Request.Scheme);

                    var template = RegisterEmailTemplate(Input);
                    try
                    {
                        await _emailService.SendEmailAsync(Input.Email, template.Subject, template.Body);
                    }
                    catch (Exception ex)
                    {
                    }
                    if (apiRequest)
                    {
                        return await TokenLogin(new LoginViewModel
                        {
                            Email = Input.Email,
                            Password = Input.Password
                        });
                    }
                    else
                    {
                        return await Login(new LoginViewModel
                        {
                            Email = Input.Email,
                            Password = Input.Password,
                            IsNew = true
                        });
                    }
                }
                else
                {
                    foreach (var error in result.Message.Split(Environment.NewLine))
                    {
                        ModelState.AddModelError(string.Empty, error);
                    }
                    if (apiRequest)
                    {
                        return BadRequest(Straightforward.Converters.Serializer.Serialize(result.Message.Split(Environment.NewLine)));
                    }
                    else
                    {
                        return View(Input);
                    }
                }
            }
            else
            {
                if (apiRequest)
                {
                    var errorMessages = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return BadRequest(Straightforward.Converters.Serializer.Serialize(errorMessages));
                }
                else
                {
                    return View(Input);
                }
            }
        }

        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string token, string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return View("Error");

            var result = await _userManager.ConfirmEmailAsync(user, token);
            return View(result.Succeeded ? "ConfirmEmail" : "Error");
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        [HttpGet]
        public IActionResult GenerateResetToken()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GenerateResetToken(string email)
        {
            var Token = "";
            int Result = 0;
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                Token = await _userManager.GeneratePasswordResetTokenAsync(user);
                Token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(Token));
                string ResetUrl = Url.Action("ResetPassword", "Account", new { Token, email = email }, Request.Scheme);

                var template = ResetEmailTemplate(email, ResetUrl);
                Result = await _emailService.SendEmailAsync(email, template.Subject, template.Body);
            }

            if (Result == 1)
                return View("MailSent", Token);
            else
            {
                ViewBag.errmsg = "Unable to send reset token. Please contact support.";
                return View();
            }
        }

        public IActionResult ResetPassword(string token)
        {
            return View(new ResetPassword { Token = token });
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPassword reset)
        {
            var user = await _userManager.FindByEmailAsync(reset.Email);
            if (user != null)
            {
                var decoded = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(reset.Token));
                var result = await _userManager.ResetPasswordAsync(user, decoded, reset.Password);
                if (!result.Succeeded)
                {
                    reset.Messages = string.Join("\n", result.Errors.Select(e => e.Description));
                    return View(reset);
                }
            }
            else
            {
                reset.Messages = "Invalid Email";
                return View(reset);
            }
            return RedirectToAction("Login");
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            string userName = "";

            var user = await _dbContext.Users.Where(x => x.UserName == model.Email || x.Email == model.Email).FirstOrDefaultAsync();
            if (user != null)
            {
                if (user.RevokeAccess)
                {
                    ModelState.AddModelError(string.Empty, "Account does not exist.");
                    return View(model);
                }
                userName = user.UserName;
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(userName, model.Password, model.RememberMe, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                /// Added claimtype policy for multiple role of single user and it configured in startup.cs policy setup
                var claims = new List<Claim>
                 {
                     new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                     new Claim(ClaimTypes.Name,user.UserName)
                };

                var roles = await _userManager.GetRolesAsync(user);

                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }
                if (roles.Contains(RunTimeElements.AdministratorRole))
                {
                    return Redirect("/Admin/");
                }
                if (model != null && model.IsNew)
                {
                    return RedirectToLocal(returnUrl, true);
                }
                else
                {
                    return RedirectToLocal(returnUrl, false);
                }
            }

            if (result.IsLockedOut)
            {
                return View("Lockout");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> TokenLogin([FromBody] LoginViewModel model)
        {
            var user = _dbContext.Users.FirstOrDefault(u => u.Email == model.Email);
            if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
            {
                var tokenHandler = new JwtSecurityTokenHandler();

                var roles = await _userManager.GetRolesAsync(user);
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new Claim[]{
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Email),
                    new Claim(ClaimTypes.Role, string.Join(',', roles)),
                    }),
                    Issuer = _contextAccessor.HttpContext.Request.Host.Host,
                    Audience = _contextAccessor.HttpContext.Request.Host.Host,
                    Expires = DateTime.Now.AddDays(7),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(RunTimeElements.JwtSecret)), SecurityAlgorithms.HmacSha512Signature),
                };
                var token = tokenHandler.CreateToken(tokenDescriptor);
                return Ok(new
                {
                    token = tokenHandler.WriteToken(token),
                    expiration = token.ValidTo //A UTC TIME FORMAT WITH T AND Z
                });
            }
            return Unauthorized();
        }

        public IActionResult AccessDenied(string returnUrl = null)
        {
            return View();
        }

        public async Task<IActionResult> Join(string r = "")
        {
            ViewData["key"] = r;
            
            if (User.Identity.IsAuthenticated)
                return RedirectToAction(nameof(CheckoutController.SelectPackage), "Checkout");

            var packages = await new StripeUtil().GetPackages();
            return View(packages);
        }

        private IActionResult RedirectToLocal(string returnUrl, bool isSignUp = false)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                if (!isSignUp)
                    return RedirectToAction(nameof(HomeController.Index), "Home");
                else
                    return RedirectToAction(nameof(CheckoutController.SelectPackage), "Checkout");
            }
        }

        [HttpGet]
        public IActionResult Affiliate(string link = "")
        {
            return View();
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordVM model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByNameAsync(User.Identity.Name);
                if (user == null)
                {
                    return RedirectToAction("Login");
                }

                var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View();
                }

                // Upon successfully changing the password refresh sign-in cookie
                await _signInManager.RefreshSignInAsync(user);
                return View("ChangePasswordConfirmation");
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            var result = false;
            if (user != null)
            {
                result = await _userService.Revoke(user.Id);
                if (result)
                {
                    await _signInManager.SignOutAsync();
                    return RedirectToAction(nameof(HomeController.Index), "Home");
                }
            }
            else
            {
                return Redirect("/MyAccount/Index");
            }
            return new JsonResult(result);
        }
    }
}