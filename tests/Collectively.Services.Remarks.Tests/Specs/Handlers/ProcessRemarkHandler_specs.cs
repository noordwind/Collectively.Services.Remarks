using System;
using It = Machine.Specifications.It;
using Moq;
using Collectively.Services.Remarks.Handlers;
using Collectively.Services.Remarks.Policies;
using Collectively.Services.Remarks.Services;
using Machine.Specifications;
using Collectively.Messages.Commands.Remarks;
using Collectively.Messages.Events.Remarks;
using System.Threading;
using RawRabbit.Pipe;
using RawRabbit;

namespace Collectively.Services.Remarks.Tests.Specs.Handlers
{
    public class ProcessRemarkHandler_specs : ChangeRemarkStateBase_specs<ProcessRemark, ProcessRemarkHandler>
    {
        protected static Mock<IProcessRemarkPolicy> ProcessRemarkPolicyMock;  
        protected static Mock<IRemarkActionService> RemarkActionService; 

        protected static void Initialize()
        {
            InitializeBase();
            ProcessRemarkPolicyMock = new Mock<IProcessRemarkPolicy>();
            RemarkActionService = new Mock<IRemarkActionService>();
            CommandHandler = new ProcessRemarkHandler(Handler,
                BusClientMock.Object, 
                RemarkServiceMock.Object,
                GroupServiceMock.Object,
                RemarkStateServiceMock.Object,
                FileResolverMock.Object,
                FileValidatorMock.Object,
                ResourceFactoryMock.Object,
                ProcessRemarkPolicyMock.Object,
                RemarkActionService.Object);
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
            RemarkServiceMock.Verify(x => x.GetAsync(Command.RemarkId), Times.Exactly(2));
        };

        It should_publish_remark_processed_event = () =>
        {
            VerifyPublishAsync(Moq.It.IsAny<RemarkProcessed>(), Times.Once);
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
            RemarkServiceMock.Verify(x => x.GetAsync(Command.RemarkId), Times.Once);
        };

        It should_not_publish_remark_processed_event = () =>
        {
            VerifyPublishAsync(Moq.It.IsAny<RemarkProcessed>(), Times.Never);
        };

        It should_publish_process_remark_rejected_message = () =>
        {
            VerifyPublishAsync(Moq.It.Is<ProcessRemarkRejected>(m =>
                    m.RequestId == Command.Request.Id
                    && m.RemarkId == Command.RemarkId
                    && m.UserId == Command.UserId
                    && m.Code == OperationCodes.Error), Times.Once);
        };
    }
}