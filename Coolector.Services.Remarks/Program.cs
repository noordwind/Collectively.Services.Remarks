using Coolector.Common.Commands.Remarks;
using Coolector.Common.Events.Users;
using Coolector.Common.Host;
using Coolector.Services.Remarks.Framework;

namespace Coolector.Services.Remarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            WebServiceHost
                .Create<Startup>(port: 10002)
                .UseAutofac(Bootstrapper.LifetimeScope)
                .UseRabbitMq(queueName: typeof(Program).Namespace)
                .SubscribeToCommand<CreateRemark>()
                .SubscribeToCommand<DeleteRemark>()
                .SubscribeToCommand<ResolveRemark>()
                .SubscribeToEvent<NewUserSignedIn>()
                .SubscribeToEvent<UserNameChanged>()
                .Build()
                .Run();
        }
    }
}
