using System;
using It = Machine.Specifications.It;
using Moq;
using Collectively.Services.Remarks.Handlers;
using Machine.Specifications;
using RawRabbit.Configuration.Publish;
using Collectively.Messages.Commands.Remarks;
using Collectively.Messages.Events.Remarks;

namespace Collectively.Services.Remarks.Tests.Specs.Handlers
{
    public class ProcessRemarkHandler_specs : ChangeRemarkStateBase_specs<ProcessRemark, ProcessRemarkHandler>
    {
        protected static void Initialize()
        {
            InitializeBase();
            CommandHandler = new ProcessRemarkHandler(Handler,
                BusClientMock.Object, 
                RemarkServiceMock.Object,
                RemarkStateServiceMock.Object,
                FileResolverMock.Object,
                FileValidatorMock.Object,
                ResourceFactoryMock.Object);
            Remark.SetProcessingState(User, Description);
        }
    }

    [Subject("ProcessRemarkHandler HandleAsync")]
    public class when_invoking_process_remark_handle_async : ProcessRemarkHandler_specs
    {
        Establish context = () => Initialize();

        Because of = () => CommandHandler.HandleAsync(Command).Await();

        It should_process_remark = () =>
        {
            RemarkStateServiceMock.Verify(x => x.ProcessAsync(Command.RemarkId, Command.UserId, Description, Location), Times.Once);
        };

        It should_fetch_remark = () =>
        {
            RemarkServiceMock.Verify(x => x.GetAsync(Command.RemarkId), Times.Once);
        };

        It should_publish_remark_processed_event = () =>
        {
            BusClientMock.Verify(x => x.PublishAsync(Moq.It.IsAny<RemarkProcessed>(), 
                Moq.It.IsAny<Guid>(), 
                Moq.It.IsAny<Action<IPublishConfigurationBuilder>>()), Times.Once);
        };
    }

    [Subject("ProcessRemarkHandler HandleAsync")]
    public class when_invoking_process_remark_handle_async_and_coordinates_are_invalid : ProcessRemarkHandler_specs
    {
        Establish context = () =>
        {
            Initialize();
            Command.Latitude = 100;
            Command.Longitude = 200;
        };

        Because of = () => CommandHandler.HandleAsync(Command).Await();

        It should_not_process_remark = () =>
        {
            RemarkStateServiceMock.Verify(x => x.ProcessAsync(Command.RemarkId, Command.UserId, Description, Location), Times.Never);
        };

        It should_not_fetch_remark = () =>
        {
            RemarkServiceMock.Verify(x => x.GetAsync(Command.RemarkId), Times.Never);
        };

        It should_not_publish_remark_processed_event = () =>
        {
            BusClientMock.Verify(x => x.PublishAsync(Moq.It.IsAny<RemarkProcessed>(),
                Moq.It.IsAny<Guid>(),
                Moq.It.IsAny<Action<IPublishConfigurationBuilder>>()), Times.Never);
        };

        It should_publish_process_remark_rejected_message = () =>
        {
            BusClientMock.Verify(x => x.PublishAsync(Moq.It.Is<ProcessRemarkRejected>(m =>
                    m.RequestId == Command.Request.Id
                    && m.RemarkId == Command.RemarkId
                    && m.UserId == Command.UserId
                    && m.Code == OperationCodes.Error),
                Moq.It.IsAny<Guid>(),
                Moq.It.IsAny<Action<IPublishConfigurationBuilder>>()), Times.Once);
        };
    }
}