using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Collectively.Common.Extensions;
using Collectively.Services.Remarks.Domain;
using Collectively.Services.Remarks.Extensions;
using Collectively.Services.Remarks.Repositories;
using NLog;

namespace Collectively.Services.Remarks.Services
{
    public class GroupService : IGroupService
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IUserRepository _userRepository;
        private readonly IGroupRepository _groupRepository;

        public GroupService(IGroupRepository groupRepository, IUserRepository userRepository)
        {
            _groupRepository = groupRepository;
            _userRepository = userRepository;
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
            var group = await _groupRepository.GetOrFailAsync(groupId);
            var user = await _userRepository.GetOrFailAsync(userId);
            ValidateCreateRemarkCriteriaOrFail(group, user);
            ValidateLocationOrFail(group, latitude, longitude);
        }

        private void ValidateCreateRemarkCriteriaOrFail(Group group, User user)
        {
            if (user.Role == "moderator" || user.Role == "administrator")
            {
                return;
            }
            string createRemarkCriteria = "";
            if(!group.Criteria.TryGetValue("create_remark", out createRemarkCriteria))
            {
                return;
            }
            if(createRemarkCriteria.Empty() || createRemarkCriteria == "public")
            {
                return;
            }
        }

        private void ValidateLocationOrFail(Group group, double latitude, double longitude)
        {
            if(group.Locations == null || !group.Locations.Any())
            {
                return;
            }
            //TODO: Implement location validation.
        }
    }
}