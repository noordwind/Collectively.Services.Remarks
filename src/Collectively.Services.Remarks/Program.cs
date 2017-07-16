using Collectively.Common.Host;
using Collectively.Services.Remarks.Framework;
using Collectively.Messages.Commands.Remarks;
using Collectively.Messages.Events.Users;

namespace Collectively.Services.Remarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            WebServiceHost
                .Create<Startup>()
                .UseAutofac(Bootstrapper.LifetimeScope)
                .UseRabbitMq(queueName: typeof(Program).Namespace)
                .SubscribeToCommand<CreateRemark>()
                .SubscribeToCommand<DeleteRemark>()
                .SubscribeToCommand<ProcessRemark>()
                .SubscribeToCommand<ResolveRemark>()
                .SubscribeToCommand<RenewRemark>()
                .SubscribeToCommand<CancelRemark>()
                .SubscribeToCommand<DeleteRemarkState>()
                .SubscribeToCommand<AddPhotoToRemark>()
                .SubscribeToCommand<AddPhotosToRemark>()
                .SubscribeToCommand<RemovePhotosFromRemark>()
                .SubscribeToCommand<SubmitRemarkVote>()
                .SubscribeToCommand<DeleteRemarkVote>()
                .SubscribeToCommand<AddCommentToRemark>()
                .SubscribeToCommand<EditRemarkComment>()
                .SubscribeToCommand<DeleteRemarkComment>()
                .SubscribeToCommand<SubmitRemarkCommentVote>()
                .SubscribeToCommand<DeleteRemarkCommentVote>()
                .SubscribeToCommand<AddFavoriteRemark>()
                .SubscribeToCommand<DeleteFavoriteRemark>()
                .SubscribeToCommand<TakeRemarkAction>()
                .SubscribeToCommand<CancelRemarkAction>()
                .SubscribeToEvent<SignedUp>()
                .SubscribeToEvent<UsernameChanged>()
                .Build()
                .Run();
        }
    }
}
