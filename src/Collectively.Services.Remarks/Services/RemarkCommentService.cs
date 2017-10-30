using System;
using System.Net;
using System.Threading.Tasks;
using Collectively.Common.Domain;
using Collectively.Common.Types;
using Collectively.Services.Remarks.Domain;
using Collectively.Services.Remarks.Extensions;
using Collectively.Services.Remarks.Repositories;
using Collectively.Services.Remarks.Settings;
using Serilog;

namespace Collectively.Services.Remarks.Services
{
  public class RemarkCommentService : IRemarkCommentService
  {
        private static readonly ILogger Logger = Log.Logger;
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
            if (comment.HasNoValue)
            {
                throw new ServiceException(OperationCodes.CommentNotFound, 
                    $"Remark comment with id: '{commentId}' was not found.");
            }

            var user = await _userRepository.GetOrFailAsync(userId);
            if (user.Role == "moderator" || user.Role == "administrator" || user.Role == "owner")
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
            var remark = await _remarkRepository.GetOrFailAsync(remarkId);
            
            return remark.GetComment(commentId);
        }

        public async Task AddAsync(Guid remarkId, Guid commentId, string userId, string text)
        {
            var remark = await _remarkRepository.GetOrFailAsync(remarkId);
            var user = await _userRepository.GetOrFailAsync(userId);
            var encodedText = WebUtility.HtmlEncode(text);
            remark.AddComment(commentId, user, encodedText);
            await _remarkRepository.UpdateAsync(remark);
        }

        public async Task EditAsync(Guid remarkId, Guid commentId, string text)
        {
            var remark = await _remarkRepository.GetOrFailAsync(remarkId);
            var encodedText = WebUtility.HtmlEncode(text);
            remark.EditComment(commentId, encodedText);
            await _remarkRepository.UpdateAsync(remark);
        }

        public async Task RemoveAsync(Guid remarkId, Guid commentId)
        {
            var remark = await _remarkRepository.GetOrFailAsync(remarkId);
            remark.RemoveComment(commentId);
            await _remarkRepository.UpdateAsync(remark);
        }

        public async Task SubmitVoteAsync(Guid remarkId, Guid commentId, string userId, bool positive, DateTime createdAt)
        {
            var remark = await _remarkRepository.GetOrFailAsync(remarkId);
            var comment = remark.GetComment(commentId);
            if (comment.HasNoValue)
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
            var remark = await _remarkRepository.GetOrFailAsync(remarkId);
            var comment = remark.GetComment(commentId);
            if (comment.HasNoValue)
            {
                throw new ServiceException(OperationCodes.CommentNotFound, 
                    $"Remark comment with id: '{commentId}' was not found.");
            }
            comment.Value.DeleteVote(userId);
            await _remarkRepository.UpdateAsync(remark);
        }
    }
}