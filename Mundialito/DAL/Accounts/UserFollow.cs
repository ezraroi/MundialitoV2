

namespace Mundialito.DAL.Accounts;

public class UserFollow
{
    public string FollowerId { get; set; }
    public MundialitoUser Follower { get; set; }

    public string FolloweeId { get; set; }
    public MundialitoUser Followee { get; set; }
}
