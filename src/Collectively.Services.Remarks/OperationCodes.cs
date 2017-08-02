namespace Collectively.Services.Remarks
{
    public static class OperationCodes
    {
        public static string Success => "success";
        public static string RemarkNotFound => "remark_not_found";
        public static string CannotCreateRemarkTooOften => "cannot_create_remark_too_often";
        public static string InvalidRemarkDescription => "invalid_remark_description";
        public static string RemarkAuthorNotProvided => "remark_author_not_provided";
        public static string RemarkCategoryNotProvided => "remark_category_not_provided";
        public static string RemarkLocationNotProvided => "remark_location_not_provided";
        public static string RemarkStateNotProvided => "remark_state_not_provided";
        public static string CommentUserNotProvided => "comment_user_not_provided";
        public static string TooManyComments => "comment_not_found";
        public static string EmptyComment => "empty_comment";
        public static string CommentNotFound => "comment_not_found";
        public static string CommentEditedTooManyTimes => "comment_edited_too_many_times";
        public static string CommentRemoved => "comment_removed";
        public static string InvalidComment => "invalid_comment";
        public static string CannotAddCommentTooOften => "cannot_add_comment_too_often";
        public static string UserNotAllowedToModifyComment => "user_not_allowed_to_modify_comment";
        public static string UserNotAllowedToModifyRemark => "user_not_allowed_to_modify_remark";
        public static string UserAlreadyParticipatesInRemark => "user_already_participates_in_remark";
        public static string UserNotFound => "user_not_found";
        public static string UserNotActive => "user_not_active";
        public static string CategoryNotFound => "category_not_found";
        public static string InactiveUser => "inactive_user";  
        public static string InvalidLocality => "invalid_locality";        
        public static string DistanceBetweenUserAndRemarkIsTooBig => "distance_between_user_and_remark_is_too_big";
        public static string FileNotFound => "file_not_found";
        public static string NoFiles => "no_files";
        public static string InvalidFile => "invalid_file";
        public static string CannotConvertFile => "cannot_convert_file";
        public static string TooManyFiles => "too_many_files";
        public static string CannotSubmitVote => "cannot_submit_vote";
        public static string CannotDeleteVote => "cannot_delete_vote";
        public static string VoteAlreadySubmitted => "vote_already_submitted";
        public static string CannotSetState => "cannot_set_state";
        public static string CannotSetStateTooOften => "cannot_set_state_too_often";
        public static string CannotRemoveNonProcessingState => "cannot_remove_non_processing_state";
        public static string UserNotAllowedToRemoveState => "user_not_allowed_to_remove_state";
        public static string StateNotFound => "state_not_found";
        public static string StateRemoved => "state_removed";
        public static string GroupMemberNotFound => "group_member_not_found";
        public static string GroupMemberNotActive => "group_member_not_active";
        public static string UnknownGroupMemberCriteria => "unknown_group_member_criteria";
        public static string InsufficientGroupMemberCriteria => "insufficient_group_member_criteria"; 
        public static string Error => "error";
    }
}