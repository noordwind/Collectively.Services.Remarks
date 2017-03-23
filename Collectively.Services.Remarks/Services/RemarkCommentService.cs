using System;
using System.Threading.Tasks;
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

        public async Task DoSomethingAsync()
        {
             await Task.CompletedTask;
        }
    }
}