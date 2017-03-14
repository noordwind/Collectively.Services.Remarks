using Collectively.Common.Domain;
using Collectively.Services.Remarks.Domain;
using Collectively.Services.Remarks.Repositories;
using Collectively.Services.Remarks.Services;
using Machine.Specifications;
using Moq;
using System;
using Collectively.Services.Remarks.Settings;
using It = Machine.Specifications.It;

namespace Collectively.Services.Remarks.Tests.Specs
{
    public abstract class RemarkStateService_specs
    {
        protected static IRemarkStateService RemarkStateService;
        protected static Mock<IRemarkRepository> RemarkRepositoryMock;
        protected static Mock<IUserRepository> UserRepositoryMock;
        protected static Mock<IRemarkPhotoService> RemarkPhotoServiceMock;
        protected static GeneralSettings GeneralSettings;
        protected static string UserId = "userId";
        protected static User User = new User(UserId, "TestUser", "user");
        protected static File File = File.Create("image.png", "image/png", new byte[] { 1, 2, 3, 4 });
        protected static Guid RemarkId = Guid.NewGuid();
        protected static Location Location = Location.Zero;
        protected static Remark Remark;
        protected static string Description;
        protected static Exception Exception;

        protected static void Initialize()
        {
            RemarkRepositoryMock = new Mock<IRemarkRepository>();
            UserRepositoryMock = new Mock<IUserRepository>();
            RemarkPhotoServiceMock = new Mock<IRemarkPhotoService>();
            GeneralSettings = new GeneralSettings
            {
                AllowedDistance = 15.0
            };

            RemarkStateService = new RemarkStateService(RemarkRepositoryMock.Object, 
                UserRepositoryMock.Object,
                RemarkPhotoServiceMock.Object,
                GeneralSettings);
            
            var user = new User(UserId, "name", "user");
            var category = new Category("category");
            Description = "test";
            Remark = new Remark(RemarkId, user, category, Location);
            Remark.AddPhoto(RemarkPhoto.Small(Guid.NewGuid(), "test.jpg", "http://my-test-image.com"));

            RemarkRepositoryMock.Setup(x => x.GetByIdAsync(Moq.It.IsAny<Guid>()))
                .ReturnsAsync(Remark);
            UserRepositoryMock.Setup(x => x.GetByUserIdAsync(Moq.It.IsAny<string>()))
                .ReturnsAsync(User);
        }
    }

    [Subject("RemarkStateService ResolveAsync")]
    public class when_resolve_async_is_invoked : RemarkStateService_specs
    {
        Establish context = () =>
        {
            Initialize();
        };

        Because of = () => RemarkStateService.ResolveAsync(RemarkId, UserId, Description, Location, File).Await();

        It should_update_remark = () =>
        {
            RemarkRepositoryMock.Verify(x => x.UpdateAsync(Moq.It.Is<Remark>(r => r.Resolved)), Times.Once);
        };

        It should_upload_file = () =>
        {
            RemarkPhotoServiceMock.Verify(x => x.UploadImagesWithDifferentSizesAsync(Remark, File, RemarkState.Names.Resolved), Times.Once);
        };
    }

    [Subject("RemarkStateService ResolveAsync")]
    public class when_resolve_async_is_invoked_and_user_does_not_exist : RemarkStateService_specs
    {
        Establish context = () =>
        {
            Initialize();
            UserRepositoryMock.Setup(x => x.GetByUserIdAsync(Moq.It.IsAny<string>()))
                .ReturnsAsync(null);
        };

        Because of = () => 
            Exception = Catch.Exception(() => RemarkStateService.ResolveAsync(RemarkId, UserId, Description, Location, File).Await());

        It should_throw_service_exception = () =>
        {
            Exception.ShouldNotBeNull();
            Exception.ShouldBeOfExactType<ServiceException>();
            Exception.Message.ShouldContain(UserId);
        };

        It should_not_update_remark = () =>
        {
            RemarkRepositoryMock.Verify(x => x.UpdateAsync(Moq.It.Is<Remark>(r => r.Resolved)), Times.Never);
        };

        It should_not_upload_file = () =>
        {
            RemarkPhotoServiceMock.Verify(x => x.UploadImagesWithDifferentSizesAsync(Remark, File, RemarkState.Names.Resolved), Times.Never);
        };
    }

    [Subject("RemarkStateService ResolveAsync")]
    public class when_resolve_async_is_invoked_and_remark_does_not_exist : RemarkStateService_specs
    {
        Establish context = () =>
        {
            Initialize();
            RemarkRepositoryMock.Setup(x => x.GetByIdAsync(Moq.It.IsAny<Guid>()))
                .ReturnsAsync(null);
        };

        Because of = () =>
            Exception = Catch.Exception(() => RemarkStateService.ResolveAsync(RemarkId, UserId, Description, Location, File).Await());

        It should_throw_service_exception = () =>
        {
            Exception.ShouldNotBeNull();
            Exception.ShouldBeOfExactType<ServiceException>();
            Exception.Message.ShouldContain(RemarkId.ToString());
        };

        It should_not_update_remark = () =>
        {
            RemarkRepositoryMock.Verify(x => x.UpdateAsync(Moq.It.Is<Remark>(r => r.Resolved)), Times.Never);
        };

        It should_not_upload_file = () =>
        {
            RemarkPhotoServiceMock.Verify(x => x.UploadImagesWithDifferentSizesAsync(Remark, File, RemarkState.Names.Resolved), Times.Never);
        };
    }

    [Subject("RemarkStateService ResolveAsync")]
    public class when_resolve_async_is_invoked_and_distance_is_too_long : RemarkStateService_specs
    {
        Establish context = () =>
        {
            Initialize();
            Location = Location.Create(40,40);
        };

        Because of = () =>
            Exception = Catch.Exception(() => RemarkStateService.ResolveAsync(RemarkId, UserId, Description, Location, File, true).Await());

        It should_throw_service_exception = () =>
        {
            Exception.ShouldNotBeNull();
            Exception.ShouldBeOfExactType<ServiceException>();
            Exception.Message.ShouldContain(RemarkId.ToString());
        };

        It should_not_update_remark = () =>
        {
            RemarkRepositoryMock.Verify(x => x.UpdateAsync(Moq.It.Is<Remark>(r => r.Resolved)), Times.Never);
        };

        It should_not_upload_file = () =>
        {
            RemarkPhotoServiceMock.Verify(x => x.UploadImagesWithDifferentSizesAsync(Remark, File, RemarkState.Names.Resolved), Times.Never);
        };
    }
}