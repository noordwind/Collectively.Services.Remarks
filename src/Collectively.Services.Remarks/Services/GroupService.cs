using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Collectively.Common.Domain;
using Collectively.Common.Extensions;
using Collectively.Common.Locations;
using Collectively.Services.Remarks.Domain;
using Collectively.Services.Remarks.Extensions;
using Collectively.Services.Remarks.Repositories;
using Serilog;

namespace Collectively.Services.Remarks.Services
{
    public class GroupService : IGroupService
    {
        private static readonly IList<string> RemarkMemberCriteria = new []{"member", "moderator", "administrator", "owner"};
        private readonly static string RemarkLocationCriterion = "remark_location";
        private static readonly ILogger Logger = Log.Logger;
        private readonly IGroupRepository _groupRepository;
        private readonly IGroupLocationRepository _groupLocationRepository;
        private readonly IGroupRemarkRepository _groupRemarkRepository;
        private readonly IUserRepository _userRepository;
        private readonly IRemarkRepository _remarkRepository;
        private readonly ILocationService _locationService;

        public GroupService(IGroupRepository groupRepository, 
            IGroupLocationRepository groupLocationRepository,
            IGroupRemarkRepository groupRemarkRepository,
            IUserRepository userRepository, 
            IRemarkRepository remarkRepository,
            ILocationService locationService)
        {
            _groupRepository = groupRepository;
            _groupLocationRepository = groupLocationRepository;
            _groupRemarkRepository = groupRemarkRepository;
            _userRepository = userRepository;
            _remarkRepository = remarkRepository;
            _locationService = locationService;
        }

        public async Task CreateIfNotFoundAsync(Guid id, string name, bool isPublic, 
            string state, string userId, IDictionary<string, ISet<string>> criteria, 
            IEnumerable<string> tags, Guid? organizationId = null)
        {
            var group = await _groupRepository.GetAsync(id);
            if (group.HasValue)
            {
                return;
            }
            Logger.Debug($"Creating a new group: '{id}', name: '{name}', public: '{isPublic}'.");
            group = new Group(id, name, isPublic, state, userId, criteria, tags, organizationId);
            await _groupRepository.AddAsync(group.Value);
            var locations = criteria.ContainsKey(RemarkLocationCriterion) ? 
                criteria[RemarkLocationCriterion] : 
                Enumerable.Empty<string>();
            await _groupLocationRepository.AddAsync(new GroupLocation(id,locations,tags));
            await _groupRemarkRepository.AddAsync(new GroupRemark(id));
        }

        public async Task ValidateIfRemarkCanBeCreatedOrFailAsync(Guid groupId, string userId,
            double latitude, double longitude)
        {
            var groupAndUser = await GetGroupAndUserAndValidateDefaultCriteriaAsync(groupId, userId, "remark_create");
            if (groupAndUser.group == null)
            {
                return;
            }
            await ValidateLocationOrFailAsync(groupAndUser.group, latitude, longitude);
        }

        public async Task ValidateIfRemarkCanBeAssignedOrFailAsync(Guid groupId, string userId,
            double latitude, double longitude)
        {
            var groupAndUser = await GetGroupAndUserAndValidateDefaultCriteriaAsync(groupId, userId, "remark_assign");
            if (groupAndUser.group == null)
            {
                return;
            }
            await ValidateLocationOrFailAsync(groupAndUser.group, latitude, longitude);
        }

        public async Task ValidateIfRemarkAssignmentCanBeRemovedOrFailAsync(Guid groupId, string userId)
            => await GetGroupAndUserAndValidateDefaultCriteriaAsync(groupId, userId, "remark_remove_assignment");

        public async Task ValidateIfRemarkCanBeResolvedOrFailAsync(Guid groupId, string userId)
            => await GetGroupAndUserAndValidateDefaultCriteriaAsync(groupId, userId, "remark_resolve");

        public async Task ValidateIfRemarkCanBeRenewedOrFailAsync(Guid groupId, string userId)
            => await GetGroupAndUserAndValidateDefaultCriteriaAsync(groupId, userId, "remark_renew");

        public async Task ValidateIfRemarkCanBeProcessedOrFailAsync(Guid groupId, string userId)
            => await GetGroupAndUserAndValidateDefaultCriteriaAsync(groupId, userId, "remark_process");

        public async Task ValidateIfRemarkCanBeCanceledOrFailAsync(Guid groupId, string userId)
            => await GetGroupAndUserAndValidateDefaultCriteriaAsync(groupId, userId, "remark_cancel");

        private async Task<(Group group,User user)> GetGroupAndUserAndValidateDefaultCriteriaAsync(Guid groupId, string userId, string criteria)
        {
            if (groupId == Guid.Empty)
            {
                return (null, null);
            }
            var group = await _groupRepository.GetOrFailAsync(groupId);
            var user = await _userRepository.GetOrFailAsync(userId);  
            ValidateRemarkCriteriaOrFail(group, user, criteria);

            return (group, user);          
        }

        private void ValidateRemarkCriteriaOrFail(Group group, User user, string operation)
        {
            var criteria = AreDefaultRemarkCriteriaMet(group, user, operation);
            if (criteria.Item1)
            {
                return;
            }
            var role = GetActiveMemberRoleOrFail(group, user);
            ValidateRemarkMemberCriteriaOrFail(criteria.Item2, role, operation);           
        }

        private Tuple<bool,ISet<string>> AreDefaultRemarkCriteriaMet(Group group, User user, string operation)
        {
            if (user.Role == "moderator" || user.Role == "administrator" || user.Role == "owner")
            {
                return new Tuple<bool,ISet<string>>(true, null);
            }
            ISet<string> criteria = null;
            if (!group.Criteria.TryGetValue(operation, out criteria))
            {
                return new Tuple<bool,ISet<string>>(true, null);
            }
            if (criteria == null || !criteria.Any() || criteria.Contains("public"))
            {
                return new Tuple<bool,ISet<string>>(true, criteria);
            }
            return new Tuple<bool,ISet<string>>(false, criteria);
        }

        private void ValidateRemarkMemberCriteriaOrFail(ISet<string> criteria, string role, string operation)
        {
            var memberRoleIndex = RemarkMemberCriteria.IndexOf(role);
            if (!criteria.Any() || memberRoleIndex < 0)
            {
                throw new ServiceException(OperationCodes.UnknownGroupMemberCriteria, 
                    $"Unknown group member criteria: '{role}', required: '{criteria}' for: '{operation}'.");                
            }
            var requiredRoleIndex = RemarkMemberCriteria.IndexOf(criteria.First());
            if (memberRoleIndex >= requiredRoleIndex)
            {
                return;
            }
            throw new ServiceException(OperationCodes.InsufficientGroupMemberCriteria, 
                $"Insufficient group member criteria: '{role}', required: '{string.Join(", ", criteria)}' for: '{operation}'.");
        }

        private async Task ValidateLocationOrFailAsync(Group group, double latitude, double longitude)
        {
            ISet<string> locations;
            if (!group.Criteria.TryGetValue(RemarkLocationCriterion, out locations))
            {
                return;
            }
            if (!locations.Any())
            {
                return;
            }
            var availableLocalities = locations.Select(x => x.ToLowerInvariant());
            var response = await _locationService.GetAsync(latitude, longitude);
            if (response.HasNoValue || response.Value.Results == null)
            {
                throw new ServiceException(OperationCodes.InvalidLocality, "Invalid locality.");
            }
            var foundLocalitiles = response.Value.Results.SelectMany(x => x.AddressComponents)
                .Where(x => x.Types.Contains("locality"))
                .Select(x => x.LongName.ToLowerInvariant());
            
            if (availableLocalities.Intersect(foundLocalitiles).Any())
            {
                return;
            }
            throw new ServiceException(OperationCodes.InvalidLocality, "Invalid locality.");
        }

        private string GetActiveMemberRoleOrFail(Group group, User user)
        {
            var member = group.Members.FirstOrDefault(x => x.UserId == user.UserId);
            if (member == null)
            {
                throw new ServiceException(OperationCodes.GroupMemberNotFound, "Group member: " + 
                    $"'{group.Name}', id: '{group.Id}', was not found '{user.Name}', id: '{user.UserId}'.");
            }
            if (!member.IsActive)
            {
                throw new ServiceException(OperationCodes.GroupMemberNotActive, "Group member: " + 
                    $"'{group.Name}', id: '{group.Id}', is not active '{user.Name}', id: '{user.UserId}'.");
            }
            return member.Role;
        }

        public async Task ValidateIfRemarkCanBeDeletedOrFailAsync(Guid groupId, 
            string userId, Guid remarkId)
        {
            var user = await _userRepository.GetOrFailAsync(userId);
            ValidateUserOrFail(user);
            var group = await _groupRepository.GetOrFailAsync(groupId);
            var memberRole = GetActiveMemberRoleOrFail(group, user);
            ValidateRemarkMemberCriteriaOrFail(null, memberRole, "remark_delete");
        }

        public async Task ValidateIfRemarkCommentCanBeDeletedOrFailAsync(Guid groupId, 
            string userId, Guid remarkId, Guid commentId)
        {
            var user = await _userRepository.GetOrFailAsync(userId);
            ValidateUserOrFail(user);
            var group = await _groupRepository.GetOrFailAsync(groupId);
            var remark = await _remarkRepository.GetOrFailAsync(remarkId);
            var comment = remark.GetCommentOrFail(commentId);
            var memberRole = GetActiveMemberRoleOrFail(group, user);
            ValidateRemarkMemberCriteriaOrFail(null, memberRole, "remark_comment_delete");
        }

        private void ValidateUserOrFail(User user)
        {
            if (user.Role == "moderator" || user.Role == "administrator" || user.Role == "owner")
            {
                return;
            }
        }

        public async Task AddMemberAsync(Guid groupId, string memberId, string role)
        {
            var group = await _groupRepository.GetOrFailAsync(groupId);
            var member = await _userRepository.GetOrFailAsync(memberId);
            group.AddMember(member.UserId, role, true);
            await _groupRepository.UpdateAsync(group);
        }

        public async Task<IEnumerable<GroupLocation>> GetGroupLocationsAsync(LocationResponse location)
        {
            if (location == null || location.Results == null || !location.Results.Any())
            {
                return Enumerable.Empty<GroupLocation>();
            }
            var locations = location.Results.SelectMany(x => x.AddressComponents)
                .Where(x => x.Types.Contains("locality"))
                .Select(x => x.LongName.ToLowerInvariant());
            
            return await _groupLocationRepository.GetAllWithLocationsAsync(locations);
        }

        public IEnumerable<GroupLocation> FilterGroupLocationsByTags(IEnumerable<GroupLocation> groupLocations, 
            IEnumerable<string> tags)
            => groupLocations.Where(x => x.Tags.Any(t => tags.Contains(t)));

        public async Task AddRemarkToGroupsAsync(Guid remarkId, IEnumerable<Guid> groupIds)
            => await _groupRemarkRepository.AddRemarksAsync(remarkId, groupIds);
    }
}