using Coolector.Common.Domain;

namespace Coolector.Services.Remarks.Domain
{
    public class UserSocialMedia : ValueObject<File>
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

        protected override bool EqualsCore(File other) => Name.Equals(other.Name);

        protected override int GetHashCodeCore() => Name.GetHashCode();
    }
}