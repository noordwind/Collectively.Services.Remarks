using Collectively.Common.Domain;

namespace Collectively.Services.Remarks.Domain
{
    public class UserSocialMedia : ValueObject<UserSocialMedia>
    {
        public string Name { get; protected set; }
        public string AccessToken { get; protected set; }

        protected UserSocialMedia()
        {
        }

        protected UserSocialMedia(string name, string accessToken)
        {
            Name = name.ToLowerInvariant();
            AccessToken = accessToken;
        }

        public static UserSocialMedia Facebok(string accessToken) => Create("facebook", accessToken);

        public static UserSocialMedia Create(string name, string accessToken) => new UserSocialMedia(name, accessToken);

        protected override bool EqualsCore(UserSocialMedia other) => Name.Equals(other.Name);

        protected override int GetHashCodeCore() => Name.GetHashCode();
    }
}