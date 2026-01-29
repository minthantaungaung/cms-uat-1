using Microsoft.Extensions.Configuration;

namespace aia_core.Services
{
    public class UserManager
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Type {get;set;}
    }
    public interface IUserService
    {
        Task<UserManager> Authenticate(string username, string password, string type);
        Task<IEnumerable<UserManager>> GetAll();
    }

    public class UserService : IUserService
    {
        private List<UserManager> _users = null;
        public UserService(IConfiguration config)
        {
            _users = new List<UserManager>();
            _users.Add(new UserManager
            {
                UserId = Guid.NewGuid().ToString(),
                UserName = config["JWT:BasicAuth:UserName"], 
                Password = config["JWT:BasicAuth:Password"],
                Type = "cms" 
            });
             _users.Add(new UserManager
            {
                UserId = Guid.NewGuid().ToString(),
                UserName = config["JWT:CustomBasicAuth:UserName"], 
                Password = config["JWT:CustomBasicAuth:Password"],
                Type = "crm" 
            });
        }

        public async Task<UserManager> Authenticate(string username, string password, string type)
        {
            var user = await Task.Run(() => _users.SingleOrDefault(x => x.UserName == username && x.Password == password && x.Type == type));

            // return null if user not found
            if (user == null)
                return null;

            // authentication successful so return user details without password
            user.Password = null;
            return user;
        }

        public async Task<IEnumerable<UserManager>> GetAll()
        {
            // return users without passwords
            return await Task.Run(() => _users.Select(x =>
            {
                x.Password = null;
                return x;
            }));
        }
    }
}
