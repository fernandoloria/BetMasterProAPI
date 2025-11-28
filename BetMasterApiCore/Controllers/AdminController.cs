using Microsoft.AspNetCore.Mvc;
using BetMasterApiCore.DbTier;
using BetMasterApiCore.Models;
using static BetMasterApiCore.Models.AdminModels;

namespace BetMasterApiCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {

        private readonly IConfiguration _configuration;

        public AdminController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("AdminLogin")]
        public AgentLoginResp AdminLogin(AgentLoginReq LoginReq)
        {
            return new LiveAdminDbClass().AdminLogin(LoginReq);
        }

        [HttpPost("GetPlayerLimitsByDGS")]
        public GetPlayerLimitsByDGSResp GetPlayerLimitsByDGS(int IdPlayer)
        {
            return new LiveAdminDbClass().GetPlayerLimitsByDGS(IdPlayer);
        }

        [HttpPost("GetAgentHierarchy")]
        public List<GetAgentHierarchyResp> GetAgentHierarchy(GetAgentHierarchyReq req)
        {
            return new LiveAdminDbClass().GetAgentHierarchy(req);
        }

        [HttpPost("GetAgentTree")]
        public List<AgentTreeResp> GetAgentTree(GetAgentHierarchyReq req)
        {
            return new LiveAdminDbClass().GetAgentTree(req);
        }

        [HttpPost("GetProfileLimits")]
        public List<ProfileLimitsResp> GetProfileLimits(List<GetProfileLimitsReq> req)
        {
            return new LiveAdminDbClass().GetProfileLimits(req);
        }

        [HttpPost("SetProfileLimits")]
        public List<LimitsRightsVerificationResp> SetProfileLimits(List<SetProfileLimitsReq> req)
        {
            return new LiveAdminDbClass().SetProfileLimits(req);
        }

        [HttpPost("SetProfileLimitsMassive")]
        public List<LimitsRightsVerificationResp> SetProfileLimitsMassive(List<SetProfileLimitsReq> req)
        {
            return new LiveAdminDbClass().SetProfileLimitsMassive(req);
        }


        [HttpPost("GetPlayersByIdAgent")]
        public List<GetPlayerListResp> GetPlayersByIdAgent(GetPlayerListReq req)
        {
            return new LiveAdminDbClass().GetPlayersByIdAgent(req);
        }

        [HttpPost("GetVerifiedPasswordByPlayer")]
        public GetVerifiedPasswordByPlayerResp GetVerifiedPasswordByPlayer(GetVerifiedPasswordByPlayerReq req)
        {
            return new LiveAdminDbClass().GetVerifiedPasswordByPlayer(req);
        }

        [HttpPost("SetVerifiedPasswordByPlayer")]
        public SetVerifiedPasswordByPlayerResp SetVerifiedPasswordByPlayer(SetVerifiedPasswordByPlayerReq req)
        {
            return new LiveAdminDbClass().SetVerifiedPasswordByPlayer(req);
        }

        [HttpPost("GetProfileLimitsByPlayer")]
        public List<ProfileLimitsByPlayerResp> GetProfileLimitsByPlayer(List<ProfileLimitsByPlayerReq> req)
        {
            return new LiveAdminDbClass().GetProfileLimitsByPlayer(req);
        }

        [HttpPost("SetProfileLimitsByPlayer")]
        public LimitsRightsVerificationResp SetProfileLimitsByPlayer(List<ProfileLimitsByPlayerReq> req)
        {
            return new LiveAdminDbClass().SetProfileLimitsByPlayer(req);
        }

        [HttpPost("GetSportsAndLeaguesHidden")]
        public List<GetSportsAndLeaguesHiddenResp> GetSportsAndLeaguesHidden(List<GetSportsAndLeaguesHiddenReq> req)
        {
            return new LiveAdminDbClass().GetSportsAndLeaguesHidden(req);
        }

        [HttpPost("SetSportsAndLeaguesHidden")]
        public SetSportsAndLeaguesHiddenResp SetSportsAndLeaguesHidden(List<SetSportsAndLeaguesHiddenReq> req)
        {
            return new LiveAdminDbClass().SetSportsAndLeaguesHidden(req);
        }

        [HttpPost("GetAccessDeniedLists")]
        public List<GetAccessDeniedListResp> GetAccessDeniedLists(List<GetAccessDeniedListReq> req)
        {
            return new LiveAdminDbClass().GetAccessDeniedLists(req);
        }

        [HttpPost("SetAccessDeniedLists")]
        public List<SetAccessDeniedListResp> SetAccessDeniedLists(List<SetAccessDeniedListReq> req)
        {
            return new LiveAdminDbClass().SetAccessDeniedLists(req);
        }

        [HttpPost("DeletePlayerLimit")]
        public List<DeletePlayerLimitResp> DeletePlayerLimit(List<DeletePlayerLimitReq> req)
        {
            return new LiveAdminDbClass().DeletePlayerLimit(req);
        }


        [HttpPost("DeleteHiddenLeagues")]
        public List<DeleteHiddenLeaguesResp> DeleteHiddenLeagues(List<DeleteHiddenLeaguesReq> req)
        {
            return new LiveAdminDbClass().DeleteHiddenLeagues(req);
        }

        [HttpPost("DeleteAgentLimit")]
        public List<DeleteAgentLimitResp> DeleteAgentLimit(List<DeleteAgentLimitReq> req)
        {
            return new LiveAdminDbClass().DeleteAgentLimit(req);
        }

        [HttpPost("GetAgentinfo")]
        public SearchAgentResp GetAgentinfo(SearchAgentReq req)
        {
            return new LiveAdminDbClass().GetAgentinfo(req);
        }


        [HttpPost("GetPlayerinfo")]
        public SearchPlayertResp GetPlayerinfo(SearchPlayertReq req)
        {
            return new LiveAdminDbClass().GetPlayerinfo(req);
        }


        [HttpGet("GetLsportsLeagues")]
        public Task<LsportsLeagues.Root> GetLsportsLeagues()
        {
            return new LiveAdminDbClass().GetLsportsLeagues();
        }

        [HttpPost("GetLsportsLeaguesByIdSport")]
        public Task<LsportsLeagues.Root> GetLsportsLeaguesByIdSport(GetLsportsLeaguesReq req)
        {
            return new LiveAdminDbClass().GetLsportsLeaguesByIdSport(req);
        }

        [HttpGet("GetLsportsSports")]
        public Task<LsportsSports.Root1> GetLsportsSports()
        {
            return new LiveAdminDbClass().GetLsportsSports();
        }

        [HttpPost("DgsLogin")]
        public DgsUserInfo DgsLogin(DgsCredentials credentials)
        {
            return new LiveAdminDbClass().DgsUserLogin(credentials);
        }

        [HttpPost("GetFixturesByDate")]
        public List<FixtureDto> GetFixturesByDate(FixtureFilter request)
        {
            return new LiveAdminDbClass().GetFixturesByDate(request);
        }


        [HttpPost("GetAgentSettings/{id}")]
        public AgentSettings GetAgentSettings(int id)
        {
            return new LiveAdminDbClass().GetAgentSettings(id);
        }

        [HttpPost("GetAgentSettingsForAdmin/{id}")]
        public AgentSettings GetAgentSettingsForAdmin(int id)
        {
            return new LiveAdminDbClass().GetAgentSettingsForAdmin(id);
        }

        [HttpPost("SaveAgentSettings")]
        public void SaveAgentSettings(AgentSettings settings)
        {
            new LiveAdminDbClass().SaveAgentSettings(settings);
        }

    }//end controller
}//end namespace
