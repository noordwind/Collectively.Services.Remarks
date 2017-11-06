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
using Collectively.Common.NancyFx;
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
using Serilog;
using RawRabbit.Configuration;
using Collectively.Common.Extensions;
using Collectively.Messages.Events.Remarks;
using Collectively.Common.Locations;
using Microsoft.Extensions.DependencyInjection;
using Autofac.Extensions.DependencyInjection;

namespace Collectively.Services.Remarks.Framework
{
    public class Bootstrapper : AutofacNancyBootstrapper
    {
        private static readonly ILogger Logger = Log.Logger;
        private static IExceptionHandler _exceptionHandler;
        private readonly IConfiguration _configuration;
        private static readonly string DecimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        private static readonly string InvalidDecimalSeparator = DecimalSeparator == "." ? "," : ".";
        private readonly IServiceCollection _services;
        public static ILifetimeScope LifetimeScope { get; private set; }

        public Bootstrapper(IConfiguration configuration, IServiceCollection services)
        {
            _configuration = configuration;
            _services = services;
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
                builder.Populate(_services);
                builder.RegisterType<CustomJsonSerializer>().As<JsonSerializer>().SingleInstance();
                var generalSettings = _configuration.GetSettings<GeneralSettings>();
                builder.RegisterInstance(_configuration.GetSettings<MongoDbSettings>()).SingleInstance();
                builder.RegisterInstance(_configuration.GetSettings<LocationSettings>()).SingleInstance();
                builder.RegisterInstance(_configuration.GetSettings<PolicySettings>()).SingleInstance();
                builder.RegisterInstance(generalSettings).SingleInstance();
                builder.RegisterInstance(AutoMapperConfig.InitializeMapper());
                builder.RegisterModule<MongoDbModule>();
                builder.RegisterType<MongoDbInitializer>().As<IDatabaseInitializer>().InstancePerLifetimeScope();
                builder.RegisterType<DatabaseSeeder>().As<IDatabaseSeeder>().InstancePerLifetimeScope();
                builder.RegisterType<RemarkRepository>().As<IRemarkRepository>().InstancePerLifetimeScope();
                builder.RegisterType<CategoryRepository>().As<ICategoryRepository>().InstancePerLifetimeScope();
                builder.RegisterType<LocalizedResourceRepository>().As<ILocalizedResourceRepository>().InstancePerLifetimeScope();
                builder.RegisterType<TagRepository>().As<ITagRepository>().InstancePerLifetimeScope();
                builder.RegisterType<UserRepository>().As<IUserRepository>().InstancePerLifetimeScope();
                builder.RegisterType<GroupRepository>().As<IGroupRepository>().InstancePerLifetimeScope();
                builder.RegisterType<GroupLocationRepository>().As<IGroupLocationRepository>().InstancePerLifetimeScope();
                builder.RegisterType<GroupRemarkRepository>().As<IGroupRemarkRepository>().InstancePerLifetimeScope();
                builder.RegisterType<ReportRepository>().As<IReportRepository>().InstancePerLifetimeScope();
                builder.RegisterType<LocalizedResourceService>().As<ILocalizedResourceService>().InstancePerLifetimeScope();
                builder.RegisterType<RemarkService>().As<IRemarkService>().InstancePerLifetimeScope();
                builder.RegisterType<RemarkStateService>().As<IRemarkStateService>().InstancePerLifetimeScope();
                builder.RegisterType<RemarkPhotoService>().As<IRemarkPhotoService>().InstancePerLifetimeScope();
                builder.RegisterType<UserService>().As<IUserService>().InstancePerLifetimeScope();
                builder.RegisterType<SocialMediaService>().As<ISocialMediaService>().InstancePerLifetimeScope();
                builder.RegisterType<RemarkCommentService>().As<IRemarkCommentService>().InstancePerLifetimeScope();
                builder.RegisterType<RemarkActionService>().As<IRemarkActionService>().InstancePerLifetimeScope();
                builder.RegisterType<GroupService>().As<IGroupService>().InstancePerLifetimeScope();
                builder.RegisterType<ReportService>().As<IReportService>().InstancePerLifetimeScope();
                builder.RegisterType<TagManager>().As<ITagManager>().InstancePerLifetimeScope();
                builder.RegisterType<TagService>().As<ITagService>().InstancePerLifetimeScope();
                builder.RegisterType<AddCommentPolicy>().As<IAddCommentPolicy>().InstancePerLifetimeScope();
                builder.RegisterType<CreateRemarkPolicy>().As<ICreateRemarkPolicy>().InstancePerLifetimeScope();
                builder.RegisterType<ProcessRemarkPolicy>().As<IProcessRemarkPolicy>().InstancePerLifetimeScope();
                builder.RegisterType<UniqueNumberGenerator>().As<IUniqueNumberGenerator>().SingleInstance();
                builder.RegisterInstance(_configuration.GetSettings<ExceptionlessSettings>()).SingleInstance();
                builder.RegisterType<ExceptionlessExceptionHandler>().As<IExceptionHandler>().SingleInstance();
                builder.RegisterType<Handler>().As<IHandler>();
                builder.RegisterModule<ServiceClientModule>();
                builder.RegisterModule(new FilesModule(_configuration));
                builder.RegisterModule<LocationModule>();
                RegisterResourceFactory(builder);

                var assembly = typeof(Startup).GetTypeInfo().Assembly;
                builder.RegisterAssemblyTypes(assembly).AsClosedTypesOf(typeof(IEventHandler<>)).InstancePerLifetimeScope();
                builder.RegisterAssemblyTypes(assembly).AsClosedTypesOf(typeof(ICommandHandler<>)).InstancePerLifetimeScope();
                SecurityContainer.Register(builder, _configuration);
                RabbitMqContainer.Register(builder, _configuration.GetSettings<RawRabbitConfiguration>());
            });
            LifetimeScope = container;
        }

        protected override void RequestStartup(ILifetimeScope container, IPipelines pipelines, NancyContext context)
        {
            pipelines.SetupTokenAuthentication(container.Resolve<IJwtTokenHandler>());
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
            _exceptionHandler = container.Resolve<IExceptionHandler>();
            Logger.Information("Collectively.Services.Remarks API has started.");
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
                [typeof(RemarkEdited)] = remarkEndpoint,
                [typeof(RemarkDeleted)] = remarkEndpoint,
                [typeof(RemarkProcessed)] = remarkEndpoint,
                [typeof(RemarkResolved)] = remarkEndpoint,
                [typeof(RemarkRenewed)] = remarkEndpoint,
                [typeof(RemarkCanceled)] = remarkEndpoint,
                [typeof(PhotosToRemarkAdded)] = remarkEndpoint,
                [typeof(PhotosFromRemarkRemoved)] = remarkEndpoint,
                [typeof(RemarkAssignedToGroup)] = remarkEndpoint,
                [typeof(RemarkAssignmentDenied)] = remarkEndpoint,
                [typeof(RemarkAssignmentRemoved)] = remarkEndpoint
            };
            builder.RegisterModule(new ResourceFactory.Module(resources));
        }
    }
}