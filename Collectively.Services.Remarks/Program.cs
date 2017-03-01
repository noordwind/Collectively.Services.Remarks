using  Collectively.Common.Host;
using Collectively.Services.Remarks.Framework;
using Collectively.Messages.Commands.Remarks;
using  Collectively.Messages.Events.Users;

namespace Collectively.Services.Remarks
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
                .SubscribeToCommand<AddPhotosToRemark>()
                .SubscribeToCommand<RemovePhotosFromRemark>()
                .SubscribeToCommand<SubmitRemarkVote>()
                .SubscribeToCommand<DeleteRemarkVote>()
                .SubscribeToEvent<SignedUp>()
                .SubscribeToEvent<UsernameChanged>()
                .Build()
                .Run();
        }
    }
}
