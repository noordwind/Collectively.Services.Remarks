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
using NLog;

namespace Collectively.Services.Remarks.Services
{
    public class GroupService : IGroupService
    {
        private static readonly IList<string> RemarkMemberCriteria = new []{"member", "moderator", "administrator", "owner"};
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IUserRepository _userRepository;
        private readonly IGroupRepository _groupRepository;
        private readonly ILocationService _locationService;


        public GroupService(IGroupRepository groupRepository, 
            IUserRepository userRepository, ILocationService locationService)
        {
            _groupRepository = groupRepository;
            _userRepository = userRepository;
            _locationService = locationService;
        }

        public async Task CreateIfNotFoundAsync(Guid id, string name, bool isPublic, 
            string state, string userId, IDictionary<string, string> criteria, 
            IEnumerable<string> locations, Guid? organizationId = null)
        {
            var group = await _groupRepository.GetAsync(id);
            if(group.HasValue)
            {
                return;
            }
            Logger.Debug($"Creating a new group: '{id}', name: '{name}', public: '{isPublic}'.");
            group = new Group(id, name, isPublic, state, userId, criteria, locations, organizationId);
            await _groupRepository.AddAsync(group.Value);
        }

        public async Task ValidateIfRemarkCanBeCreatedOrFailAsync(Guid groupId, string userId,
            double latitude, double longitude)
        {
            if(groupId == Guid.Empty)
            {
                return;
            }
            var group = await _groupRepository.GetOrFailAsync(groupId);
            var user = await _userRepository.GetOrFailAsync(userId);
            ValidateRemarkCriteriaOrFail(group, user, "create_remark");
            await ValidateLocationOrFailAsync(group, latitude, longitude);
        }

        public async Task ValidateIfRemarkCanBeResolvedOrFailAsync(Guid groupId, string userId)
        {
            if(groupId == Guid.Empty)
            {
                return;
            }
            var group = await _groupRepository.GetOrFailAsync(groupId);
            var user = await _userRepository.GetOrFailAsync(userId);
            ValidateRemarkCriteriaOrFail(group, user, "resolve_remark");
        }

        private void ValidateRemarkCriteriaOrFail(Group group, User user, string operation)
        {
            var criteria = AreDefaultRemarkCriteriaMet(group, user, operation);
            if(criteria.Item1)
            {
                return;
            }
            var role = GetActiveMemberRoleOrFail(group, user);
            ValidateRemarkMemberCriteriaOrFail(criteria.Item2, role, operation);           
        }

        private Tuple<bool,string> AreDefaultRemarkCriteriaMet(Group group, User user, string operation)
        {
            if(user.State != "active")
            {
                return new Tuple<bool,string>(false, string.Empty);
            }
            if (user.Role == "moderator" || user.Role == "administrator")
            {
                return new Tuple<bool,string>(true, string.Empty);
            }
            var criteria = "";
            if(!group.Criteria.TryGetValue(operation, out criteria))
            {
                return new Tuple<bool,string>(true, string.Empty);
            }
            if(criteria.Empty() || criteria == "public")
            {
                return new Tuple<bool,string>(true, criteria);
            }
            return new Tuple<bool,string>(false, criteria);
        }

        private void ValidateRemarkMemberCriteriaOrFail(string criteria, string role, string operation)
        {
            var criteriaIndex = RemarkMemberCriteria.IndexOf(criteria);
            var roleIndex = RemarkMemberCriteria.IndexOf(role);
            if(criteriaIndex < 0 || roleIndex < 0)
            {
                throw new ServiceException(OperationCodes.UnknownGroupMemberCriteria, 
                    $"Unknown group member criteria: '{role}', required: '{criteria}' for: '{operation}'.");                
            }
            if(criteriaIndex <= roleIndex)
            {
                return;
            }
            throw new ServiceException(OperationCodes.InsufficientGroupMemberCriteria, 
                $"Insufficient group member criteria: '{role}', required: '{criteria}' for: '{operation}'.");
        }

        private async Task ValidateLocationOrFailAsync(Group group, double latitude, double longitude)
        {
            if(group.Locations == null || !group.Locations.Any())
            {
                return;
            }
            var availableLocalities = group.Locations.Select(x => x.ToLowerInvariant());
            var response = await _locationService.GetAsync(latitude, longitude);
            if(response.HasNoValue || response.Value.Results == null)
            {
                throw new ServiceException(OperationCodes.InvalidLocality, "Invalid locality.");
            }
            var foundLocalitiles = response.Value.Results.SelectMany(x => x.AddressComponents)
                .Where(x => x.Types.Contains("locality"))
                .Select(x => x.LongName.ToLowerInvariant());
            
            if(availableLocalities.Intersect(foundLocalitiles).Any())
            {
                return;
            }
            throw new ServiceException(OperationCodes.InvalidLocality, "Invalid locality.");
        }

        private string GetActiveMemberRoleOrFail(Group group, User user)
        {
            if(user.State != "active")
            {
                throw new ServiceException(OperationCodes.UserNotActive,
                    $"'User is not active '{user.Name}', id: '{user.UserId}'.");
            }
            var member = group.Members.FirstOrDefault(x => x.UserId == user.UserId);
            if(member == null)
            {
                throw new ServiceException(OperationCodes.GroupMemberNotFound, "Group member: " + 
                    $"'{group.Name}', id: '{group.Id}', was not found '{user.Name}', id: '{user.UserId}'.");
            }
            if(!member.IsActive)
            {
                throw new ServiceException(OperationCodes.GroupMemberNotActive, "Group member: " + 
                    $"'{group.Name}', id: '{group.Id}', is not active '{user.Name}', id: '{user.UserId}'.");
            }
            return member.Role;
        }
    }
}