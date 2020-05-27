using AtlasCity.TimeProof.Abstractions;
using AtlasCity.TimeProof.Abstractions.Helpers;
using AtlasCity.TimeProof.Abstractions.Repository;
using AtlasCity.TimeProof.Abstractions.Services;
using AtlasCity.TimeProof.Api.Extensions;
using AtlasCity.TimeProof.Common.Lib.Helpers;
using AtlasCity.TimeProof.Common.Lib.Services;
using AtlasCity.TimeProof.Repository.CosmosDb;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nethereum.RPC.NonceServices;
using Nethereum.Web3;
using Nethereum.Web3.Accounts.Managed;
using Serilog;
using Stripe;
using System.Net;
using System.Net.Mail;

namespace AtlasCity.TimeProof.Api
{
    public class Startup
    {
        readonly string MyAllowSpecificOrigins = "AllowAllHeaders";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public Startup(IWebHostEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            var origins = Configuration.GetValueAsArray("AllowedOrigins", ",");
            services.AddCors(options =>
            {
                options.AddPolicy(MyAllowSpecificOrigins,
                builder =>
                {
                    builder.WithOrigins(origins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
                });
            });

            services.AddControllers();

            Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(Configuration).CreateLogger();
            services.AddSingleton(Log.Logger);

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<ISystemDateTime, SystemDateTime>();

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            services.AddAuthentication(options =>
            {
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(jwtOptions =>
            {
                jwtOptions.Authority = Configuration.GetValue("Authentication:Authority");
                jwtOptions.Events = new JwtBearerEvents { };
                jwtOptions.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidAudiences = Configuration.GetValueAsList("Authentication:Audiences", ","),
                    ValidIssuers = Configuration.GetValueAsList("Authentication:Issuers", ","),
                };
            });

            var secretKey = Configuration.GetValue("NetheriumAccountSecretKey");
            var accountAddress = Configuration.GetValue("NetheriumAccount:FromAddress");
            var nodeEndpoint = Configuration.GetValue("NetheriumAccount:NodeEndpoint");
            var account = new Nethereum.Web3.Accounts.Account(secretKey);

            var web3 = new Web3(account, nodeEndpoint);
            account.NonceService = new InMemoryNonceService(accountAddress, web3.Client);

            services.AddSingleton<IWeb3>(web3);

            var storageAccountConnectionString = Configuration.GetConnectionString("StorageAccount");
            services.AddSingleton<ITimestampQueueService>(new TimestampQueueService(storageAccountConnectionString, Log.Logger));

            var endpointUrl = Configuration.GetValue("TransationDbEndpointUrl");
            var authorizationKey = Configuration.GetValue("TransationDbAuthorizationKey");
            services.AddSingleton<ITimestampRepository>(new TimestampRepository(endpointUrl, authorizationKey));
            services.AddSingleton<IUserRepository>(new UserRepository(endpointUrl, authorizationKey));
            services.AddSingleton<IPricePlanRepository>(new PricePlanRepository(endpointUrl, authorizationKey));
            services.AddSingleton<IAddressNonceRepository>(new AddressNonceRepository(endpointUrl, authorizationKey));
            services.AddSingleton<IPaymentRepository>(new PaymentRepository(endpointUrl, authorizationKey));
            services.AddSingleton<IPendingMembershipChangeRepository>(new PendingMembershipChangeRepository(endpointUrl, authorizationKey));

            var client = new SmtpClient(Configuration.GetValue("SMTPEmail:HostName"), int.Parse(Configuration.GetValue("SMTPEmail:Port")))
            {
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(Configuration.GetValue("SMTPEmail:UserName"), Configuration.GetValue("SMTPEmailPassword")),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                EnableSsl = true
            };

            services.AddSingleton<IEmailService>(new EmailService(client, Log.Logger));
            services.AddSingleton<IEthClient, EthClient>();

            var toAddress = Configuration.GetValue("NetheriumAccount:ToAddress");
            var networkName = Configuration.GetValue("NetheriumAccount:Network");
            var gasStationAPIEndpoint = Configuration.GetValue("NetheriumAccount:GasStationAPIEndpoint");

            var ethSetting = new EthSettings { ToAddress = toAddress, SecretKey = secretKey, Network = networkName, GasStationAPIEndpoint = gasStationAPIEndpoint };

            services.AddSingleton<IEthHelper>(provider => new EthHelper(ethSetting, provider.GetService<IEthClient>()));

            var timeProofLoginUri = Configuration.GetValue("TimeProofLoginUri");
            services.AddSingleton<IEmailTemplateHelper>(new EmailTemplateHelper(timeProofLoginUri));

            var paymentApiKey = Configuration.GetValue("StripePaymentApiKey");
            var stripeClient = new StripeClient(paymentApiKey);
            services.AddSingleton(new PaymentIntentService(stripeClient));
            services.AddSingleton(new CustomerService(stripeClient));
            services.AddSingleton(new PaymentMethodService(stripeClient));
            services.AddSingleton(new SetupIntentService(stripeClient));

            services.AddSingleton<IPaymentService, StripePaymentService>();
            services.AddSingleton<IUserService, UserService>();
            services.AddSingleton<IUserSubscriptionService, UserSubscriptionService>();
            services.AddSingleton<ITimestampService, TimestampService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger logger)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseCors(MyAllowSpecificOrigins);

            app.UseSerilogRequestLogging();
            app.ConfigureExceptionHandler(logger);

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }     
    }
}
