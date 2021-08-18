using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Goalvisor.Data;
using Goalvisor.Models;
using Goalvisor.Policies;
using Goalvisor.Services.Affiliate;
using Goalvisor.Services.Email;
using Goalvisor.Services.Roles;
using Goalvisor.Services.Subscriptions;
using Goalvisor.Services.Users;
using Goalvisor.ViewModels.Core;
using Straightforward.Converters;
using System;
using System.Net;
using System.Security.Claims;
using System.Text;

namespace Goalvisor
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            RunTimeElements.JwtSecret = Configuration.GetValue<string>("JwtSecret");
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    Configuration.GetConnectionString("DefaultConnection")));

            services.AddIdentity<ApplicationUser, IdentityRole<int>>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Lockout = new LockoutOptions
                {
                    DefaultLockoutTimeSpan = TimeSpan.FromDays(30),
                    MaxFailedAccessAttempts = 3
                };
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
                {
                    options.Cookie.HttpOnly = true;
                    options.ExpireTimeSpan = TimeSpan.FromDays(3);
                    options.AccessDeniedPath = "/Account/AccessDenied";
                    options.LoginPath = "/Account/Login";
                    options.LogoutPath = "/Account/Logout";
                    options.SlidingExpiration = true;
                })
                .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.SaveToken = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(RunTimeElements.JwtSecret)),
                        ValidateIssuer = false,
                        ValidateAudience = false
                    };
                });
            services.AddAuthorization(o =>
            {
                o.AddPolicy(RunTimeElements.SubscriberOrAdminPolicy, p => p.Requirements.Add(new ActiveSubscription()));

                o.AddPolicy(RunTimeElements.AdministratorRole, policy => { policy.RequireClaim(ClaimTypes.Role, RunTimeElements.AdministratorRole); });
            });
            // This configures on how fast security rules gets applied on logged users,
            services.Configure<SecurityStampValidatorOptions>(options =>
            {
                options.ValidationInterval = TimeSpan.FromSeconds(10);
            });

            services.AddControllersWithViews()
                .AddRazorRuntimeCompilation()
                .AddJsonOptions(o =>
                {
                    o.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                    o.JsonSerializerOptions.AllowTrailingCommas = true;
                    o.JsonSerializerOptions.Converters.Add(new QuotedIntConverter());
                    o.JsonSerializerOptions.Converters.Add(new QuotedDoubleConverter());
                    o.JsonSerializerOptions.Converters.Add(new QuotedFloatConverter());
                    o.JsonSerializerOptions.Converters.Add(new QuotedLongConverter());
                    o.JsonSerializerOptions.Converters.Add(new IsoDateTimeConverter("yyyy-MM-ddTHH:mm:ss"));
                    o.JsonSerializerOptions.Converters.Add(new QuotedIntConverterNullable());
                    o.JsonSerializerOptions.Converters.Add(new QuotedDoubleConverterNullable());
                    o.JsonSerializerOptions.Converters.Add(new QuotedFloatConverterNullable());
                    o.JsonSerializerOptions.Converters.Add(new QuotedLongConverterNullable());
                    o.JsonSerializerOptions.Converters.Add(new IsoDateTimeConverterNullable("yyyy-MM-ddTHH:mm:ss"));
                    o.JsonSerializerOptions.IgnoreNullValues = true;
                });

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<IAuthorizationHandler, ActiveSubscriptionPolicy>();
            services.AddTransient<UserManager<ApplicationUser>, UserManager<ApplicationUser>>();
            services.AddTransient<SignInManager<ApplicationUser>, SignInManager<ApplicationUser>>();
            services.AddScoped<ISubscriptionsService, SubscriptionsService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IRolesService, RolesService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddTransient<IAffiliateService, AffiliateService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseStatusCodePages(async context =>
            {
                var response = context.HttpContext.Response;

                if (response.StatusCode == (int)HttpStatusCode.Unauthorized || response.StatusCode == (int)HttpStatusCode.Forbidden)
                    response.Redirect("/Authentication");
            });
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}