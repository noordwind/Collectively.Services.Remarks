using System;
using System.Threading.Tasks;
using Collectively.Common.Domain;
using Collectively.Common.Extensions;
using Collectively.Services.Remarks.Domain;
using Collectively.Services.Remarks.Repositories;

namespace Collectively.Services.Remarks.Extensions
{
    public static class RepositoryExtensions
    {
        public static async Task<Remark> GetOrFailAsync(this IRemarkRepository repository, Guid remarkId)
            => await repository
                .GetByIdAsync(remarkId)
                .UnwrapAsync(noValueException: new ServiceException(OperationCodes.RemarkNotFound,
                    $"Remark with id: '{remarkId}' does not exist!"));

        public static async Task<User> GetOrFailAsync(this IUserRepository repository,string userId)
            => await repository
                .GetByUserIdAsync(userId)
                .UnwrapAsync(noValueException: new ServiceException(OperationCodes.UserNotFound,
                    $"User with id: '{userId}' does not exist!"));

        public static async Task<Group> GetOrFailAsync(this IGroupRepository repository, Guid groupId)
            => await repository
                .GetAsync(groupId)
                .UnwrapAsync(noValueException: new ServiceException(OperationCodes.GroupNotFound,
                    $"Group with id: '{groupId}' does not exist!"));
    }
}