using Backend.Models;
using Domain;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Backend.Controllers
{
    public class GenericController : Controller
    {
        private DataContextLocal db = new DataContextLocal();

        public JsonResult GetTeams(int leagueId)
        {
            List<TeamList> teamsViews = new List<TeamList>();
            db.Configuration.ProxyCreationEnabled = false;
            var teams = db.Teams.Where(m => m.LeagueId == leagueId).ToList();

            foreach(var item in teams)
            {
                teamsViews.Add(new TeamList()
                {
                    TeamId = item.TeamId,
                    LeagueId = item.LeagueId,
                    Name = item.Name,
                    Logo = item.Logo,
                    Initials = item.Initials
                });
            }

            return Json(teamsViews, JsonRequestBehavior.AllowGet);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            } 

            base.Dispose(disposing);
        }
    }
}