namespace Collectively.Services.Remarks
{
    public static class OperationCodes
    {
        public static string Success => "success";
        public static string RemarkNotFound => "remark_not_found";
        public static string InvalidRemarkDescription => "invalid_remark_description";
        public static string RemarkAuthorNotProvided => "remark_author_not_provided";
        public static string RemarkCategoryNotProvided => "remark_category_not_provided";
        public static string RemarkLocationNotProvided => "remark_location_not_provided";
        public static string RemarkStateNotProvided => "remark_state_not_provided";
        public static string UserNotAllowedToModifyRemark => "user_not_allowed_to_modify_remark";
        public static string UserNotFound => "user_not_found";
        public static string CategoryNotFound => "category_not_found";
        public static string InactiveUser => "inactive_user";        
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
        public static string Error => "error";
    }
}