using System;
using System.Reflection;
using System.Collections.Generic;
using System.Globalization;
using Autofac;
using Collectively.Common.Files;
using Collectively.Messages.Commands;
using Collectively.Messages.Events;
using Collectively.Common.Exceptionless;
using Collectively.Common.Mongo;
using Collectively.Common.Nancy;
using Collectively.Common.RabbitMq;
using Collectively.Common.Security;
using Collectively.Common.Services;
using Collectively.Common.ServiceClients;
using Collectively.Services.Remarks.Policies;
using Collectively.Services.Remarks.Settings;
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
using Collectively.Messages.Events.Remarks;


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
                builder.RegisterType<CustomJsonSerializer>().As<JsonSerializer>().SingleInstance();
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
                builder.RegisterType<RemarkStateService>().As<IRemarkStateService>();
                builder.RegisterType<RemarkPhotoService>().As<IRemarkPhotoService>();
                builder.RegisterType<UserService>().As<IUserService>();
                builder.RegisterType<SocialMediaService>().As<ISocialMediaService>();
                builder.RegisterType<RemarkCommentService>().As<IRemarkCommentService>();
                builder.RegisterType<RemarkActionService>().As<IRemarkActionService>();
                builder.RegisterType<AddCommentPolicy>().As<IAddCommentPolicy>();
                builder.RegisterType<CreateRemarkPolicy>().As<ICreateRemarkPolicy>();
                builder.RegisterType<ProcessRemarkPolicy>().As<IProcessRemarkPolicy>();
                builder.RegisterType<UniqueNumberGenerator>().As<IUniqueNumberGenerator>().SingleInstance();
                builder.RegisterInstance(_configuration.GetSettings<ExceptionlessSettings>()).SingleInstance();
                builder.RegisterType<ExceptionlessExceptionHandler>().As<IExceptionHandler>().SingleInstance();
                builder.RegisterType<Handler>().As<IHandler>();
                builder.RegisterModule<ServiceClientModule>();
                builder.RegisterModule(new FilesModule(_configuration));
                RegisterResourceFactory(builder);

                var assembly = typeof(Startup).GetTypeInfo().Assembly;
                builder.RegisterAssemblyTypes(assembly).AsClosedTypesOf(typeof(IEventHandler<>));
                builder.RegisterAssemblyTypes(assembly).AsClosedTypesOf(typeof(ICommandHandler<>));
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

        private void RegisterResourceFactory(ContainerBuilder builder)
        {
            var remarkEndpoint = "remarks/{0}";
            var resources = new Dictionary<Type, string>
            {
                [typeof(RemarkCreated)] = remarkEndpoint,
                [typeof(RemarkDeleted)] = remarkEndpoint,
                [typeof(RemarkProcessed)] = remarkEndpoint,
                [typeof(RemarkResolved)] = remarkEndpoint,
                [typeof(RemarkRenewed)] = remarkEndpoint,
                [typeof(RemarkCanceled)] = remarkEndpoint,
                [typeof(PhotosToRemarkAdded)] = remarkEndpoint,
                [typeof(PhotosFromRemarkRemoved)] = remarkEndpoint
            };
            builder.RegisterModule(new ResourceFactory.Module(resources));
        }
    }
}