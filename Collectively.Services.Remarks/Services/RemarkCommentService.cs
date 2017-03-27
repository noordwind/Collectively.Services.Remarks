using System;
using System.Threading.Tasks;
using Collectively.Common.Domain;
using Collectively.Common.Types;
using Collectively.Services.Remarks.Domain;
using Collectively.Services.Remarks.Repositories;
using Collectively.Services.Remarks.Settings;
using NLog;

namespace Collectively.Services.Remarks.Services
{
  public class RemarkCommentService : IRemarkCommentService
  {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IRemarkRepository _remarkRepository;
        private readonly IUserRepository _userRepository;
        private readonly GeneralSettings _settings;

        public RemarkCommentService(IRemarkRepository remarkRepository, 
            IUserRepository userRepository,
            GeneralSettings settings)
        {
            _remarkRepository = remarkRepository;
            _userRepository = userRepository;
            _settings = settings;
        }

        public async Task ValidateEditorAccessOrFailAsync(Guid remarkId, Guid commentId, string userId)
        {
            var comment = await GetAsync(remarkId, commentId);
            if(comment.HasNoValue)
            {
                throw new ServiceException(OperationCodes.CommentNotFound, 
                    $"Remark comment with id: '{commentId}' was not found.");
            }

            var user = await GetUserOrFailAsync(userId);
            if (user.Role == "moderator" || user.Role == "administrator")
            {
                return;
            }
            if (comment.Value.User.UserId != user.UserId)
            {
                throw new ServiceException(OperationCodes.UserNotAllowedToModifyComment,
                    $"User with id: '{userId}' is not allowed" +
                    $"to modify the remark comment with id: '{commentId}'.");
            }
            if (comment.Value.Removed)
            {
                throw new ServiceException(OperationCodes.CommentRemoved, 
                    $"Remark comment with id: '{commentId}' was removed.");
            }
        }

        public async Task<Maybe<Comment>> GetAsync(Guid remarkId, Guid commentId)
        {
            var remark = await GetRemarkOrFailAsync(remarkId);
            
            return remark.GetComment(commentId);
        }

        public async Task AddAsync(Guid remarkId, Guid commentId, string userId, string text)
        {
            var remark = await GetRemarkOrFailAsync(remarkId);
            var user = await GetUserOrFailAsync(userId);
            remark.AddComment(commentId, user, text);
            await _remarkRepository.UpdateAsync(remark);
        }

        public async Task EditAsync(Guid remarkId, Guid commentId, string text)
        {
            var remark = await GetRemarkOrFailAsync(remarkId);
            remark.EditComment(commentId, text);
            await _remarkRepository.UpdateAsync(remark);
        }

        public async Task RemoveAsync(Guid remarkId, Guid commentId)
        {
            var remark = await GetRemarkOrFailAsync(remarkId);
            remark.RemoveComment(commentId);
            await _remarkRepository.UpdateAsync(remark);
        }

        public async Task SubmitVoteAsync(Guid remarkId, Guid commentId, string userId, bool positive, DateTime createdAt)
        {
            var remark = await GetRemarkOrFailAsync(remarkId);
            var comment = remark.GetComment(commentId);
            if(comment.HasNoValue)
            {
                throw new ServiceException(OperationCodes.CommentNotFound, 
                    $"Remark comment with id: '{commentId}' was not found.");
            }
            if (positive)
            {
                comment.Value.VotePositive(userId, createdAt);
            } 
            else
            {
                comment.Value.VoteNegative(userId, createdAt);
            }
            await _remarkRepository.UpdateAsync(remark);
        }

        public async Task DeleteVoteAsync(Guid remarkId, Guid commentId, string userId)
        {
            var remark = await GetRemarkOrFailAsync(remarkId);
            var comment = remark.GetComment(commentId);
            if(comment.HasNoValue)
            {
                throw new ServiceException(OperationCodes.CommentNotFound, 
                    $"Remark comment with id: '{commentId}' was not found.");
            }
            comment.Value.DeleteVote(userId);
            await _remarkRepository.UpdateAsync(remark);
        }

        private async Task<Remark> GetRemarkOrFailAsync(Guid remarkId)
        {
            var remark = await _remarkRepository.GetByIdAsync(remarkId);
            if (remark.HasNoValue)
            {
                throw new ServiceException(OperationCodes.RemarkNotFound,
                    $"Remark with id: '{remarkId}' does not exist!");
            }

            return remark.Value;
        }

        private async Task<User> GetUserOrFailAsync(string userId)
        {
            var user = await _userRepository.GetByUserIdAsync(userId);
            if (user.HasNoValue)
            {
                throw new ServiceException(OperationCodes.UserNotFound,
                    $"User with id: '{userId}' does not exist!");
            }

            return user.Value;
        }
    }
}