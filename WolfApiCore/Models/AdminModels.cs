namespace WolfApiCore.Models
{
    public class AdminModels
    {

        public class AgentLoginReq
        {
            public string Username { get; set; }
            public string Password { get; set; }
            public DateTime LoginAt { get; set; }
            public int Origin { get; set; }
        }

        public class AgentLoginResp
        {
            public int IdUser { get; set; }
            public string LoginName { get; set; }
            public string Password { get; set; }
            public int IdAgent { get; set; }
            public string Agent { get; set; }
            public bool IsDistributor { get; set; }
            public bool OnlineAccess { get; set; }
            public int Distributor { get; set; }
            public bool Enable { get; set; }
        }

        public class AccessDeniedReq
        {
            public int AgentId { get; set; }
            public int PlayerId { get; set; }
            public bool Enable { get; set; }
        }

        public class AccessDeniedResp
        {
            public int Id { get; set; }
            public int AgentId { get; set; }
            public int PlayerId { get; set; }
            public bool Enable { get; set; }
            public DateTime ModifiedAt { get; set; }
        }

        public class AgentAccessStatusReq
        {
            public int AgentId { get; set; }
            public int PlayerId { get; set; }
            public bool Enable { get; set; }
        }

        public class AgentAccessStatusResp
        {
            public int Id { get; set; }
            public int AgentId { get; set; }
            public int PlayerId { get; set; }
            public bool Enable { get; set; }
            public DateTime ModifiedAt { get; set; }
        }

        public class SportsAndLeaguesHiddenReq
        {
            public int AgentId { get; set; }
            public int SportId { get; set; }
            public int LeagueId { get; set; }
            public string? SportName { get; set; }
            public string? LeagueName { get; set; }
            public bool Enable { get; set; }
        }

        public class SportsAndLeaguesHiddenResp
        {
            public int Id { get; set; }
            public int AgentId { get; set; }
            public int SportId { get; set; }
            public int LeagueId { get; set; }
            public string? SportName { get; set; }
            public string? LeagueName { get; set; }
            public bool Enable { get; set; }
            public DateTime ModifiedAt { get; set; }
        }

        public class GetPlayerLimitsByDGSResp
        {
            public string Player { get; set; }
            public int IdPlayer { get; set; }
            public int IdLineType { get; set; }
            public int IdProfile { get; set; }
            public decimal OnlineMinWager { get; set; }
            public decimal OnlineMaxWager { get; set; }
            public decimal PL_MaxPayout { get; set; }
        }

        public class GetPlayerProfile_GetInfoResp
        {
            public decimal PL_MaxPayout { get; set; }
        }

        public class GetAgentHierarchyResp
        {
            public int AgentID { get; set; }
            public string Agent { get; set; }
            public int Depth { get; set; }
        }

        public class GetAgentHierarchyReq
        {
            public int? IdAgent { get; set; }
        }

        public class AgentTreeResp
        {
            public int IdAgent { get; set; }
            public string Agent { get; set; }
            public int AgentLevel { get; set; }
        }

        public class GetProfileLimitsReq
        {
            public int AgentId { get; set; }
            public int IdWagerType { get; set; }
            public int? SportId { get; set; }
            public int? LeagueId { get; set; }
            public string? SportName { get; set; }
            public string? LeagueName { get; set; }
            public int? FixtureId { get; set; }
            public decimal MaxWager { get; set; }
            public decimal MinWager { get; set; }
            public decimal MaxPayout { get; set; }
            public decimal MinPayout { get; set; }

            public decimal MinPrice { get; set; }
            public decimal MaxPrice { get; set; }
            public decimal TotAmtGame { get; set; }

            public DateTime ModifiedAt { get; set; }
        }

        public class SetProfileLimitsReq
        {
            public int Id { get; set; }
            public int AgentId { get; set; }
            public int IdWagerType { get; set; }
            public int? SportId { get; set; }
            public int? LeagueId { get; set; }
            public string? SportName { get; set; }
            public string? LeagueName { get; set; }
            public int? FixtureId { get; set; }
            public decimal MaxWager { get; set; }
            public decimal MinWager { get; set; }
            public decimal MaxPayout { get; set; }
            public decimal MinPayout { get; set; }
            public decimal MinPrice { get; set; }
            public decimal MaxPrice { get; set; }
            public decimal TotAmtGame { get; set; }

        }

        public class ProfileLimitsResp
        {
            public int Id { get; set; }
            public int AgentId { get; set; }
            public int IdWagerType { get; set; }
            public int? SportId { get; set; }
            public int? LeagueId { get; set; }
            public string? SportName { get; set; }
            public string? LeagueName { get; set; }
            public int? FixtureId { get; set; }
            public decimal MaxWager { get; set; }
            public decimal MinWager { get; set; }
            public decimal MaxPayout { get; set; }
            public decimal MinPayout { get; set; }
            public decimal MinPrice { get; set; }
            public decimal MaxPrice { get; set; }
            public decimal TotAmtGame { get; set; }
            public DateTime ModifiedAt { get; set; }
        }

        public class LimitsRightsVerificationResp
        {
            public int AgentId { get; set; }
            public int Code { get; set; }
            public string Message { get; set; }
            public bool Applied { get; set; }
            public decimal MaxWager { get; set; }
            public decimal MinWager { get; set; }
            public decimal MaxPayout { get; set; }
            public decimal MinPrice { get; set; }
            public decimal MaxPrice { get; set; }
            public decimal TotAmtGame { get; set; }
        }

        public class GetPlayerListReq
        {
            public int IdAgent { get; set; }
        }

        public class GetPlayerListResp
        {
            public int IdPlayer { get; set; }
            public string Player { get; set; }
            public decimal AvailBalance { get; set; }
            public decimal CurrentBalance { get; set; }
            public decimal AmountAtRisk { get; set; }
        }

        public class ProfileLimitsByPlayerReq
        {

            public int PlayerId { get; set; }
            public int IdWagerType { get; set; }
            public int? SportId { get; set; }
            public int? LeagueId { get; set; }
            public string? SportName { get; set; }
            public string? LeagueName { get; set; }
            public int? FixtureId { get; set; }
            public decimal MaxWager { get; set; }
            public decimal MinWager { get; set; }
            public decimal MaxPayout { get; set; }
            public decimal MinPayout { get; set; }
            public decimal MinPrice { get; set; }
            public decimal MaxPrice { get; set; }
            public decimal TotAmtGame { get; set; }
        }

        public class ProfileLimitsByPlayerResp
        {
            public int Id { get; set; }
            public int PlayerId { get; set; }
            public int IdWagerType { get; set; }
            public int? SportId { get; set; }
            public int? LeagueId { get; set; }
            public string? SportName { get; set; }
            public string? LeagueName { get; set; }
            public int? FixtureId { get; set; }
            public decimal MaxWager { get; set; }
            public decimal MinWager { get; set; }
            public decimal MaxPayout { get; set; }
            public decimal MinPayout { get; set; }

            public decimal MinPrice { get; set; }
            public decimal MaxPrice { get; set; }
            public decimal TotAmtGame { get; set; }
            public string? Type { get; set; }

            public DateTime ModifiedAt { get; set; }
        }

        public class GetVerifiedPasswordByPlayerResp
        {
            public int PlayerId { get; set; }
            public bool CheckPassword { get; set; }
            public DateTime ModifiedAt { get; set; }
        }

        public class GetVerifiedPasswordByPlayerReq
        {
            public int PlayerId { get; set; }
        }

        public class SetVerifiedPasswordByPlayerReq
        {
            public int PlayerId { get; set; }
            public bool CheckPassword { get; set; }
        }

        public class SetVerifiedPasswordByPlayerResp
        {
            public int Code { get; set; }
            public string Message { get; set; }
            public DateTime ModifiedAt { get; set; }
        }

        public class SetSportsAndLeaguesHiddenReq
        {
            public int AgentId { get; set; }
            public int? PlayerId { get; set; }
            public int SportId { get; set; }
            public int? LeagueId { get; set; }
            public string? SportName { get; set; }
            public string? LeagueName { get; set; }
            public bool Enable { get; set; }

        }

        public class SetSportsAndLeaguesHiddenResp
        {
            public int Code { get; set; }
            public string Message { get; set; }
            public int AgentId { get; set; }
            public int? PlayerId { get; set; }
            public int SportId { get; set; }
            public int? LeagueId { get; set; }
            public string? SportName { get; set; }
            public string? LeagueName { get; set; }
            public bool Enable { get; set; }
            public DateTime ModifiedAt { get; set; }
        }

        public class GetSportsAndLeaguesHiddenResp
        {
            public int Id { get; set; }
            public int AgentId { get; set; }
            public int? PlayerId { get; set; }
            public int SportId { get; set; }
            public int? LeagueId { get; set; }
            public string? SportName { get; set; }
            public string? LeagueName { get; set; }
            public bool Enable { get; set; }
            public DateTime ModifiedAt { get; set; }
        }

        public class GetSportsAndLeaguesHiddenReq
        {
            public int AgentId { get; set; }
            public int? PlayerId { get; set; }
            
            public int SportId { get; set; }
            public int? LeagueId { get; set; }
            public string? SportName { get; set; }
            public string? LeagueName { get; set; }
            
        }

        public class GetAccessDeniedListReq
        {
            public int? AgentId { get; set; }
            public int? PlayerId { get; set; }
            public bool AllData { get; set; }
        }

        public class GetAccessDeniedListResp
        {
            public int Id { get; set; }
            public int? AgentId { get; set; }
            public int? PlayerId { get; set; }
            public bool Enable { get; set; }
            public DateTime ModifiedAt { get; set; }
        }

        public class SetAccessDeniedListReq
        {
            public int Id { get; set; }
            public int? AgentId { get; set; }
            public int? PlayerId { get; set; }
            public bool Enable { get; set; }
        }

        public class SetAccessDeniedListResp
        {
            public int Id { get; set; }
            public int Code { get; set; }
            public string Message { get; set; }
            public int? AgentId { get; set; }
            public int? PlayerId { get; set; }
            public bool Enable { get; set; }
        }

        public class DeletePlayerLimitReq 
        {
            public int Id { get; set; }
            public int? PlayerId { get; set; }

        }

        public class DeletePlayerLimitResp
        {
            public int Code { get; set; }
            public string Message { get; set; }
            public DateTime ModifiedAt { get; set; }
        }

        public class DeleteAgentLimitReq
        {
            public int AgentId { get; set; }
            public int IdWagerType { get; set; }
            public int SportId { get; set; }
            public int? LeagueId { get; set; }

        }

        public class DeleteAgentLimitResp
        {
            public int Code { get; set; }
            public string Message { get; set; }
            public DateTime ModifiedAt { get; set; }
        }

        public class DeleteHiddenLeaguesReq
        {
            public int Id { get; set; }
            public int? AgentId { get; set; }

        }

        public class DeleteHiddenLeaguesResp
        {
            public int Code { get; set; }
            public string Message { get; set; }
            public DateTime ModifiedAt { get; set; }
        }

        public class SearchAgentReq {
            public string Agent { get; set; }
        }

        public class SearchPlayertReq
        {
            public string Player { get; set; }
        }

        public class SearchAgentResp
        {
            public string AgentInfo { get; set; }
        }

        public class SearchPlayertResp
        {
            public int IdPlayer { get; set; }
            public int IdAgent { get; set; }
            public string  Player { get; set; }
        }

        public class GetLsportsLeaguesReq {
            public int[] IdSports { get; set; }
        }

        public class GetPlayerInfoReq {
            public int PlayerId { get; set; }
        }

        public class GetPlayerInfoResp {
            public int IdAgent { get; set; }
        }
        
        public class AgentSettings
        {
            public int IdAgent { get; set; }
            public int? SecondsDelay { get; set; }
        }

    }
}
