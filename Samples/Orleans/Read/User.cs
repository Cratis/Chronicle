using Concepts.Users;

namespace Read.Users;

public class User
{
    public UserId Id { get; set; } = UserId.NotSet;
    public ProfileName Name { get; set; } = ProfileName.NotSet;
    public UserName UserName { get; set; } = UserName.NotSet;
    public UserPassword Password { get; set; } = UserPassword.NotSet;
    public bool System { get; set; }
    public bool Onboarded { get; set; }
    public IEnumerable<UserGroup> Groups { get; set; } = [];
    public IEnumerable<UserRole> Roles { get; set; } = [];
}
