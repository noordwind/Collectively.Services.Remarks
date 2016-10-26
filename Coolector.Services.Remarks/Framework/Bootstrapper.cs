using System;
using System.Reflection;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Amazon;
using Amazon.S3;
using Autofac;
using Coolector.Common.Commands;
using Coolector.Common.Events;
using Coolector.Common.Mongo;
using Coolector.Common.Nancy;
using Coolector.Services.Remarks.Repositories;
using Coolector.Services.Remarks.Services;
using Microsoft.Extensions.Configuration;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Configuration;
using NLog;
using RawRabbit;
using RawRabbit.vNext;
using RawRabbit.Configuration;
using Coolector.Common.Extensions;
using Coolector.Services.Remarks.Settings;
using Polly;
using RabbitMQ.Client.Exceptions;

namespace Coolector.Services.Remarks.Framework
{
    public class Bootstrapper : AutofacNancyBootstrapper
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IConfiguration _configuration;
        private static readonly string DecimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        private static readonly string InvalidDecimalSeparator = DecimalSeparator == "." ? "," : ".";
        public static ILifetimeScope LifetimeScope { get; private set; }

        public Bootstrapper(IConfiguration configuration)
        {
            _configuration = configuration;
        }

#if DEBUG
        public override void Configure(INancyEnvironment environment)
        {
            base.Configure(environment);
            environment.Tracing(enabled: false, displayErrorTraces: true);
        }
#endif

        protected override void ConfigureApplicationContainer(ILifetimeScope container)
        {
            base.ConfigureApplicationContainer(container);

            var rmqRetryPolicy = Policy
                .Handle<ConnectFailureException>()
                .Or<BrokerUnreachableException>()
                .Or<IOException>()
                .WaitAndRetry(5, retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timeSpan, retryCount, context) => {
                        Logger.Error(exception, $"Cannot connect to RabbitMQ. retryCount:{retryCount}, duration:{timeSpan}");
                    }
                );

            container.Update(builder =>
            {
                var generalSettings = _configuration.GetSettings<GeneralSettings>();
                builder.RegisterInstance(_configuration.GetSettings<MongoDbSettings>()).SingleInstance();
                builder.RegisterInstance(generalSettings).SingleInstance();
                builder.RegisterModule<MongoDbModule>();
                builder.RegisterType<MongoDbInitializer>().As<IDatabaseInitializer>();
                builder.RegisterType<DatabaseSeeder>().As<IDatabaseSeeder>();
                builder.RegisterType<RemarkRepository>().As<IRemarkRepository>();
                builder.RegisterType<CategoryRepository>().As<ICategoryRepository>();
                builder.RegisterType<CategoryRepository>().As<ICategoryRepository>();
                builder.RegisterType<UserRepository>().As<IUserRepository>();
                builder.RegisterType<RemarkService>().As<IRemarkService>();
                builder.RegisterType<UserService>().As<IUserService>();
                builder.RegisterType<ImageService>().As<IImageService>();
                builder.RegisterType<FileValidator>().As<IFileValidator>().SingleInstance();
                builder.RegisterType<FileResolver>().As<IFileResolver>().SingleInstance();
                var rawRabbitConfiguration = _configuration.GetSettings<RawRabbitConfiguration>();
                builder.RegisterInstance(rawRabbitConfiguration).SingleInstance();
                rmqRetryPolicy.Execute(() => builder
                        .RegisterInstance(BusClientFactory.CreateDefault(rawRabbitConfiguration))
                        .As<IBusClient>()
                );
                ConfigureStorage(builder);
                var coreAssembly = typeof(Startup).GetTypeInfo().Assembly;
                builder.RegisterAssemblyTypes(coreAssembly).AsClosedTypesOf(typeof(IEventHandler<>));
                builder.RegisterAssemblyTypes(coreAssembly).AsClosedTypesOf(typeof(ICommandHandler<>));
            });
            LifetimeScope = container;
        }

        protected override void ApplicationStartup(ILifetimeScope container, IPipelines pipelines)
        {
            var databaseSettings = container.Resolve<MongoDbSettings>();
            var databaseInitializer = container.Resolve<IDatabaseInitializer>();
            databaseInitializer.InitializeAsync();
            if (databaseSettings.Seed)
            {
                var databaseSeeder = container.Resolve<IDatabaseSeeder>();
                databaseSeeder.SeedAsync();
            }

            pipelines.BeforeRequest += (ctx) =>
            {
                FixNumberFormat(ctx);

                return null;
            };
            pipelines.AfterRequest += (ctx) =>
            {
                ctx.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                ctx.Response.Headers.Add("Access-Control-Allow-Methods", "POST,PUT,GET,OPTIONS,DELETE");
                ctx.Response.Headers.Add("Access-Control-Allow-Headers",
                    "Authorization, Origin, X-Requested-With, Content-Type, Accept");
            };
            Logger.Info("Coolector.Services.Remarks API Started");
        }

        private void ConfigureStorage(ContainerBuilder builder)
        {
            builder.RegisterInstance(_configuration.GetSettings<AwsS3Settings>()).SingleInstance();
            builder.Register(c =>
                {
                    var settings = c.Resolve<AwsS3Settings>();

                    return new AmazonS3Client(settings.AccessKey, settings.SecretKey,
                        RegionEndpoint.GetBySystemName(settings.Region));
                })
                .As<IAmazonS3>();
            builder.RegisterType<AwsS3FileHandler>().As<IFileHandler>();
        }

        private void FixNumberFormat(NancyContext ctx)
        {
            if (ctx.Request.Query == null)
                return;

            var fixedNumbers = new Dictionary<string, double>();
            foreach (var key in ctx.Request.Query)
            {
                var value = ctx.Request.Query[key].ToString();
                if (!value.Contains(InvalidDecimalSeparator))
                    continue;

                var number = 0;
                if (int.TryParse(value.Split(InvalidDecimalSeparator[0])[0], out number))
                    fixedNumbers[key] = double.Parse(value.Replace(InvalidDecimalSeparator, DecimalSeparator));
            }
            foreach (var fixedNumber in fixedNumbers)
            {
                ctx.Request.Query[fixedNumber.Key] = fixedNumber.Value;
            }
        }
    }
}