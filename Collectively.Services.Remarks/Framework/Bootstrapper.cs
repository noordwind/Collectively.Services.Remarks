using System;
using System.Reflection;
using System.Collections.Generic;
using System.Globalization;
using Amazon;
using Amazon.S3;
using Autofac;
using Collectively.Messages.Commands;
using Collectively.Messages.Events;
using Collectively.Common.Exceptionless;
using Collectively.Common.Mongo;
using Collectively.Common.Nancy;
using Nancy.Serialization.JsonNet;
using Collectively.Common.RabbitMq;
using Collectively.Common.Security;
using Collectively.Services.Remarks.Repositories;
using Collectively.Services.Remarks.Services;
using Microsoft.Extensions.Configuration;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Bootstrappers.Autofac;
using Nancy.Configuration;
using Newtonsoft.Json;
using NLog;
using RawRabbit.Configuration;
using Collectively.Common.Extensions;
using Collectively.Common.Services;
using Collectively.Services.Remarks.Settings;

namespace Collectively.Services.Remarks.Framework
{
    public class Bootstrapper : AutofacNancyBootstrapper
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static IExceptionHandler _exceptionHandler;
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

            container.Update(builder =>
            {
                builder.RegisterType<JsonNetSerializer>().As<JsonSerializer>().SingleInstance();
                var generalSettings = _configuration.GetSettings<GeneralSettings>();
                builder.RegisterInstance(_configuration.GetSettings<MongoDbSettings>()).SingleInstance();
                builder.RegisterInstance(generalSettings).SingleInstance();
                builder.RegisterInstance(AutoMapperConfig.InitializeMapper());
                builder.RegisterModule<MongoDbModule>();
                builder.RegisterType<MongoDbInitializer>().As<IDatabaseInitializer>();
                builder.RegisterType<DatabaseSeeder>().As<IDatabaseSeeder>();
                builder.RegisterType<RemarkRepository>().As<IRemarkRepository>();
                builder.RegisterType<CategoryRepository>().As<ICategoryRepository>();
                builder.RegisterType<LocalizedResourceRepository>().As<ILocalizedResourceRepository>();
                builder.RegisterType<TagRepository>().As<ITagRepository>();
                builder.RegisterType<UserRepository>().As<IUserRepository>();
                builder.RegisterType<LocalizedResourceService>().As<ILocalizedResourceService>();
                builder.RegisterType<RemarkService>().As<IRemarkService>();
                builder.RegisterType<UserService>().As<IUserService>();
                builder.RegisterType<ImageService>().As<IImageService>();
                builder.RegisterType<SocialMediaService>().As<ISocialMediaService>();
                builder.RegisterType<FileValidator>().As<IFileValidator>().SingleInstance();
                builder.RegisterType<FileResolver>().As<IFileResolver>().SingleInstance();
                builder.RegisterType<UniqueNumberGenerator>().As<IUniqueNumberGenerator>().SingleInstance();
                builder.RegisterInstance(_configuration.GetSettings<ExceptionlessSettings>()).SingleInstance();
                builder.RegisterType<ExceptionlessExceptionHandler>().As<IExceptionHandler>().SingleInstance();
                builder.RegisterType<Handler>().As<IHandler>();

                var assembly = typeof(Startup).GetTypeInfo().Assembly;
                builder.RegisterAssemblyTypes(assembly).AsClosedTypesOf(typeof(IEventHandler<>));
                builder.RegisterAssemblyTypes(assembly).AsClosedTypesOf(typeof(ICommandHandler<>));

                ConfigureStorage(builder);
                SecurityContainer.Register(builder, _configuration);
                RabbitMqContainer.Register(builder, _configuration.GetSettings<RawRabbitConfiguration>());
            });
            LifetimeScope = container;
        }

        protected override void RequestStartup(ILifetimeScope container, IPipelines pipelines, NancyContext context)
        {
            pipelines.OnError.AddItemToEndOfPipeline((ctx, ex) =>
            {
                _exceptionHandler.Handle(ex, ctx.ToExceptionData(),
                    "Request details", "Collectively", "Service", "Remarks");

                return ctx.Response;
            });
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
            pipelines.SetupTokenAuthentication(container);
            _exceptionHandler = container.Resolve<IExceptionHandler>();
            Logger.Info("Collectively.Services.Remarks API has started.");
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