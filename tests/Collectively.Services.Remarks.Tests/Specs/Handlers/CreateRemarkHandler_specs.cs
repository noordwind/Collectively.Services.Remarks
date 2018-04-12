﻿using Collectively.Common.Files;
using Collectively.Services.Remarks.Domain;
using Collectively.Services.Remarks.Handlers;
using Collectively.Services.Remarks.Policies;
using Collectively.Services.Remarks.Services;
using Machine.Specifications;
using Moq;
using RawRabbit;
using System;
using Collectively.Messages.Commands;
using Collectively.Common.Services;
using Collectively.Messages.Commands.Remarks;
using Collectively.Messages.Events.Remarks;
using It = Machine.Specifications.It;
using System.Collections.Generic;
using RawRabbit.Pipe;
using System.Threading;
using Collectively.Common.Locations;

namespace Collectively.Services.Remarks.Tests.Specs.Handlers
{
    public abstract class CreateRemarkHandler_specs : SpecsBase
    {
        protected static CreateRemarkHandler CreateRemarkHandler;
        protected static IHandler Handler;
        protected static Mock<IFileResolver> FileResolverMock;
        protected static Mock<IFileValidator> FileValidatorMock;
        protected static Mock<IRemarkService> RemarkServiceMock;
        protected static Mock<IGroupService> GroupServiceMock;
        protected static Mock<ISocialMediaService> SocialMediaServiceMock;
        protected static Mock<ILocationService> LocationServiceMock;
        protected static Mock<IExceptionHandler> ExceptionHandlerMock;
        protected static Mock<IResourceFactory> ResourceFactoryMock;
        protected static Mock<ICreateRemarkPolicy> CreateRemarkPolicyMock;
        protected static CreateRemark Command;
        protected static Exception Exception;

        protected static void Initialize()
        {
            InitializeBus();
            ExceptionHandlerMock = new Mock<IExceptionHandler>();
            Handler = new Handler(ExceptionHandlerMock.Object);
            FileResolverMock = new Mock<IFileResolver>();
            FileValidatorMock = new Mock<IFileValidator>();
            RemarkServiceMock = new Mock<IRemarkService>();
            GroupServiceMock = new Mock<IGroupService>();
            SocialMediaServiceMock = new Mock<ISocialMediaService>();
            LocationServiceMock = new Mock<ILocationService>();
            ResourceFactoryMock = new Mock<IResourceFactory>();
            CreateRemarkPolicyMock = new Mock<ICreateRemarkPolicy>();
            Command = new CreateRemark
            {
                RemarkId = Guid.NewGuid(),
                Request = new Request
                {
                    Name = "create_remark",
                    Id = Guid.NewGuid(),
                    CreatedAt = DateTime.Now,
                    Origin = "test",
                    Resource = ""
                },
                UserId = "userId",
                Category = "litter",
                Longitude = 1,
                Latitude = 1,
                Description = "test",
                Address = "address",
                Photo = new Collectively.Messages.Commands.Models.File
                {
                    Base64 = "base64",
                    Name = "file.png",
                    ContentType = "image/png"
                }
            };
            LocationServiceMock.Setup(x => x.GetAsync(Moq.It.IsAny<double>(),Moq.It.IsAny<double>()))
                .ReturnsAsync(new LocationResponse{ Results = new List<LocationResult>{new LocationResult{FormattedAddress = Command.Address}} });
            CreateRemarkHandler = new CreateRemarkHandler(Handler, BusClientMock.Object, FileResolverMock.Object,
                FileValidatorMock.Object, RemarkServiceMock.Object, GroupServiceMock.Object, 
                SocialMediaServiceMock.Object, LocationServiceMock.Object, ResourceFactoryMock.Object, CreateRemarkPolicyMock.Object);
        }
    }

    [Subject("CreateRemarkHandler HandleAsync")]
    public class when_invoking_create_remark_handle_async : CreateRemarkHandler_specs
    {
        static Location Location;
        static File File;
        static Remark Remark;

        Establish context = () =>
        {
            Initialize();
            Location = Location.Create(Command.Latitude, Command.Longitude, Command.Address);
            File = File.Create(Command.Photo.Name, Command.Photo.ContentType, new byte[] { 0x1 });
            Remark = new Remark(Guid.NewGuid(), new User(Command.UserId, "user", "user"),
                new Category("test"), Location, Command.Description);
            FileResolverMock.Setup(x => x.FromBase64(Moq.It.IsAny<string>(),
                Moq.It.IsAny<string>(), Moq.It.IsAny<string>())).Returns(File);
            FileValidatorMock.Setup(x => x.IsImage(Moq.It.IsAny<File>())).Returns(true);
            RemarkServiceMock.Setup(x => x.GetAsync(Moq.It.IsAny<Guid>())).ReturnsAsync(Remark);
        };

        Because of = () => CreateRemarkHandler.HandleAsync(Command).Await();

        It should_call_create_async_on_remark_service = () =>
        {
            RemarkServiceMock.Verify(x => x.CreateAsync(Moq.It.IsAny<Guid>(), Command.UserId,
                Command.Category, Location, Command.Description, Moq.It.IsAny<IEnumerable<string>>(), Command.GroupId, 
                null, null, null, null), Times.Once);
        };

        It should_publish_remark_created_event = () =>
        {
            VerifyPublishAsync(Moq.It.IsAny<RemarkCreated>(), Times.Once);
        };
    }

    [Subject("CreateRemarkHandler HandleAsync")]
    public class when_invoking_create_remark_handle_async_and_create_remark_fails : CreateRemarkHandler_specs
    {
        static Location Location;
        static File File;
        static Remark Remark;

        Establish context = () =>
        {
            Initialize();
            Location = Location.Create(Command.Latitude, Command.Longitude, Command.Address);
            File = File.Create(Command.Photo.Name, Command.Photo.ContentType, new byte[] { 0x1 });
            Remark = new Remark(Guid.NewGuid(), new User(Command.UserId, "user", "user"),
                new Category("test"), Location, Command.Description);
            FileResolverMock.Setup(x => x.FromBase64(Moq.It.IsAny<string>(),
                Moq.It.IsAny<string>(), Moq.It.IsAny<string>())).Returns(File);
            FileValidatorMock.Setup(x => x.IsImage(Moq.It.IsAny<File>())).Returns(true);
            RemarkServiceMock.Setup(x => x.CreateAsync(Moq.It.IsAny<Guid>(), Moq.It.IsAny<string>(),
                    Moq.It.IsAny<string>(), Moq.It.IsAny<Location>(), Moq.It.IsAny<string>(),
                    Moq.It.IsAny<IEnumerable<string>>(), Moq.It.IsAny<Guid?>(), null, null, null, null))
                .Throws<Exception>();
        };

        Because of = () => CreateRemarkHandler.HandleAsync(Command).Await();

        It should_call_create_async_on_remark_service = () =>
        {
            RemarkServiceMock.Verify(x => x.CreateAsync(Moq.It.IsAny<Guid>(), Command.UserId,
                Command.Category, Location, Command.Description, Moq.It.IsAny<IEnumerable<string>>(), Command.GroupId,
                null, null, null, null), Times.Once);
        };

        It should_not_publish_remark_created_event = () =>
        {
            VerifyPublishAsync(Moq.It.IsAny<RemarkCreated>(), Times.Never);
        };

        It should_publish_create_remark_rejected_message = () =>
        {
            VerifyPublishAsync(Moq.It.Is<CreateRemarkRejected>(m =>
                    m.RequestId == Command.Request.Id
                    && m.RemarkId == Command.RemarkId
                    && m.UserId == Command.UserId
                    && m.Code == OperationCodes.Error), Times.Once);
        };
    }
}