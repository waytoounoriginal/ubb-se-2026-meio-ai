namespace ubb_se_2026_meio_ai.Features.PersonalityMatch.Models
{
    /// <summary>
    /// Represents a user account entry in the account switcher panel.
    /// Owner: Madi
    /// </summary>
    public class UserAccountModel
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FacebookAccount { get; set; } = string.Empty;
    }
}
