namespace Coolector.Services.Remarks
{
    public static class OperationCodes
    {
        public static string Success => "success";
        public static string RemarkNotFound => "remark_not_found";
        public static string UserNotAllowedToDeleteRemark => "user_not_allowed_to_delete_remark";
        public static string DistanceBetweenUserAndRemarkIsTooBig => "distance_between_user_and_remark_is_too_big";
        public static string InvalidFile => "invalid_fille";
        public static string CannotConvertFile => "cannot_convert_file";
        public static string Error => "error";
    }
}