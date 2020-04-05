using Backend.Helpers;
using Backend.Models;
using Domain;
using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Backend.Controllers
{
    [Authorize]
    public class TournamentsController : Controller
    {
        private DataContextLocal db = new DataContextLocal();        

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> CloseMatch(Match match)
        {
            using (var transacction = db.Database.BeginTransaction())
            {
                try
                {
                    // Update match
                    var oldMatch = await db.Matches.FindAsync(match.MatchId);
                    oldMatch.LocalGoals = match.LocalGoals;
                    oldMatch.VisitorGoals = match.VisitorGoals;
                    oldMatch.StatusId = 2;
                    db.Entry(oldMatch).State = EntityState.Modified;

                    var statusMatch = GetStatus(match.LocalGoals.Value, match.VisitorGoals.Value);

                    // Update tournaments statistics
                    var local = await db.TournamentTeams
                        .Where(tt => tt.TournamentGroupId == oldMatch.TournamentGroupId &&
                                        tt.TeamId == oldMatch.LocalId)
                        .FirstOrDefaultAsync();

                    var visitor = await db.TournamentTeams
                        .Where(tt => tt.TournamentGroupId == oldMatch.TournamentGroupId &&
                                        tt.TeamId == oldMatch.VisitorId)
                        .FirstOrDefaultAsync();

                    local.MatchesPlayed++;
                    local.FavorGoals += oldMatch.LocalGoals.Value;
                    local.AgainstGoals += oldMatch.VisitorGoals.Value;

                    visitor.MatchesPlayed++;
                    visitor.FavorGoals += oldMatch.VisitorGoals.Value;
                    visitor.AgainstGoals += oldMatch.LocalGoals.Value;

                    if (statusMatch == 1)   //// Local won
                    {
                        local.MatchesWon++;
                        local.Points += 3;
                        visitor.MatchesLost++;
                    }
                    else if (statusMatch == 2)    //// Visitor won
                    {
                        visitor.MatchesWon++;
                        visitor.Points += 3;
                        local.MatchesLost++;
                    }
                    else
                    {
                        local.MatchesTied++;
                        visitor.MatchesTied++;
                        local.Points++;
                        visitor.Points++;
                    }

                    db.Entry(local).State = EntityState.Modified;
                    db.Entry(visitor).State = EntityState.Modified;
                    await db.SaveChangesAsync();

                    // Update positions
                    var teams = await db.TournamentTeams
                        .Where(tt => tt.TournamentGroupId == oldMatch.TournamentGroupId)
                        .ToListAsync();
                    var i = 1;
                    foreach (var team in teams.OrderByDescending(t => t.Points)
                                                .ThenByDescending(t => t.FavorGoals - t.AgainstGoals)
                                                .ThenByDescending(t => t.FavorGoals))
                    {
                        team.Position = i;
                        db.Entry(team).State = EntityState.Modified;
                        i++;
                    }

                    // Update predictions
                    var predictions = await db.Predictions.Where(p => p.MatchId == oldMatch.MatchId).ToListAsync();
                    foreach (var prediction in predictions)
                    {
                        var points = 0;
                        if (prediction.LocalGoals == oldMatch.LocalGoals &&
                            prediction.VisitorGoals == oldMatch.VisitorGoals)
                        {
                            points = 3;
                        }
                        else
                        {
                            var statusPrediction = GetStatus(prediction.LocalGoals, prediction.VisitorGoals);
                            if (statusMatch == statusPrediction)
                            {
                                points = 1;
                            }
                        }

                        if (points != 0)
                        {
                            prediction.Points = points;
                            db.Entry(prediction).State = EntityState.Modified;
                        }

                        // Update user
                        var user = await db.Users.FindAsync(prediction.UserId);
                        user.Points += points;
                        db.Entry(user).State = EntityState.Modified;

                        // Update points in groups
                        ////var groupUsers = await db.GroupUsers.Where(gu => gu.UserId == user.UserId &&
                        ////                                        gu.IsAccepted &&
                        ////                                        !gu.IsBlocked)
                        ////                                        .ToListAsync();
                        ////foreach (var groupUser in groupUsers)
                        ////{
                        ////    groupUser.Points += points;
                        ////    db.Entry(groupUser).State = EntityState.Modified;
                        ////}
                    }

                    await db.SaveChangesAsync();
                    transacction.Commit();
                    return RedirectToAction(string.Format("DetailsDate/{0}", oldMatch.DateId));
                }
                catch (Exception ex)
                {
                    transacction.Rollback();
                    ModelState.AddModelError(string.Empty, ex.Message);
                    return View(match);
                }
            }
        }

        private int GetStatus(int localGoals, int visitorGoals)
        {
            if (localGoals > visitorGoals)
            {
                return 1; // Local win
            }

            if (visitorGoals > localGoals)
            {
                return 2; // Visitor win
            }

            return 3; // Draw
        }

        public async Task<ActionResult> CloseMatch(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var match = await db.Matches.FindAsync(id);

            if (match == null)
            {
                return HttpNotFound();
            }

            if (match.StatusId == 3)
            {
                return RedirectToAction(string.Format("DetailsDate/{0}", match.DateId));
            }
            
            return View(match);
        }

        // GET: Matches/Edit/5
        public async Task<ActionResult> EditMatch(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            ////Match match = await db.Matches.FindAsync(id);
            var match = await db.Matches.FindAsync(id);

            if (match == null)
            {
                return HttpNotFound();
            }

            ViewBag.DateId = new SelectList(db.Dates, "DateId", "Name", match.DateId);
            ViewBag.LocalId = new SelectList(db.Teams, "TeamId", "Name", match.LocalId);
            ViewBag.StatusId = new SelectList(db.Status, "StatusId", "Name", match.StatusId);
            ViewBag.TournamentGroupId = new SelectList(db.TournamentGroups, "TournamentGroupId", "Name", match.TournamentGroupId);
            ViewBag.RefereeId = new SelectList(db.Referees, "RefereeId", "Name", match.RefereeId);
            ViewBag.VisitorId = new SelectList(db.Teams, "TeamId", "Name", match.VisitorId);
            var view = ToMatchView(match);
            return View(view);
        }

        private MatchView ToMatchView(Match match)
        {
            return new MatchView
            {
                Date = match.Date,
                DateTime = match.DateTime,
                StatusId = match.StatusId,
                Status = match.Status,
                TournamentGroup = match.TournamentGroup,
                TournamentGroupId = match.TournamentGroupId,
                Local = match.Local,
                Visitor = match.Visitor,
                LocalGoals = match.LocalGoals,
                VisitorGoals = match.VisitorGoals,
                LocalId = match.LocalId,
                VisitorId = match.VisitorId,
                DateId = match.DateId,
                LocalLeagueId = match.LocalId,
                VisitorLeagueId = match.VisitorId,
                MatchId = match.MatchId,
                Predictions = match.Predictions,
                RefereeId = match.RefereeId,
                Referee = match.Referee,
                FieldId = match.FieldId,
                Field = match.Field
            };
        }

        // POST: Matches/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditMatch(Match match)
        {
            if (ModelState.IsValid)
            {
                ////var match = ToMatch(view);
                db.Entry(match).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction(string.Format("DetailsDate/{0}", match.DateId));
            }

            ViewBag.DateId = new SelectList(db.Dates, "DateId", "Name", match.DateId);
            ViewBag.LocalId = new SelectList(db.Teams, "TeamId", "Name", match.LocalId);
            ViewBag.StatusId = new SelectList(db.Status, "StatusId", "Name", match.StatusId);
            ViewBag.TournamentGroupId = new SelectList(db.TournamentGroups, "TournamentGroupId", "Name", match.TournamentGroupId);
            ViewBag.RefereeId = new SelectList(db.Referees, "RefereeId", "Name", match.RefereeId);
            ViewBag.VisitorId = new SelectList(db.Teams, "TeamId", "Name", match.VisitorId);
            return View(match);
        }

        // GET: Matches/Create
        public async Task<ActionResult> CreateMatch(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var date = await db.Dates.FindAsync(id);

            if (date == null)
            {
                return HttpNotFound();
            }

            ViewBag.LocalLeagueId = new SelectList(db.Leagues.OrderBy(t => t.Name), "LeagueId", "Name");
            ViewBag.LocalId = new SelectList(db.Teams.Where(t => t.LeagueId == db.Leagues.FirstOrDefault().LeagueId).OrderBy(t => t.Name), "TeamId", "Name");

            ViewBag.VisitorLeagueId = new SelectList(db.Leagues.OrderBy(t => t.Name), "LeagueId", "Name");
            ViewBag.VisitorId = new SelectList(db.Teams.Where(t => t.LeagueId == db.Leagues.FirstOrDefault().LeagueId).OrderBy(t => t.Name), "TeamId", "Name");

            ViewBag.TournamentGroupId = new SelectList(db.TournamentGroups.Where(tg => tg.TournamentId == date.TournamentId).OrderBy(tg => tg.Name), "TournamentGroupId", "Name");            
            ViewBag.RefereeId = new SelectList(db.Referees.OrderBy(tg => tg.Name), "RefereeId", "Name");
            ViewBag.FieldId = new SelectList(db.Fields.OrderBy(tg => tg.Name), "FieldId", "Name");
            var view = new MatchView { DateId = date.DateId, };
            return View(view);
        }

        // POST: Matches/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateMatch(MatchView view)
        {
            if (ModelState.IsValid)
            {
                view.StatusId = 1;                
                view.DateTime = Convert.ToDateTime(string.Format("{0} {1}", view.DateString, view.TimeString));
                var match = ToMatch(view);
                db.Matches.Add(match);
                await db.SaveChangesAsync();
                return RedirectToAction(string.Format("DetailsDate/{0}", view.DateId));
            }

            var date = await db.Dates.FindAsync(view.DateId);

            ViewBag.LocalLeagueId = new SelectList(db.Leagues.OrderBy(t => t.Name), "LeagueId", "Name", view.LocalLeagueId);
            ViewBag.LocalId = new SelectList(db.Teams.Where(t => t.LeagueId == view.LocalLeagueId).OrderBy(t => t.Name), "TeamId", "Name", view.LocalId);
            ViewBag.VisitorLeagueId = new SelectList(db.Leagues.OrderBy(t => t.Name), "LeagueId", "Name", view.VisitorLeagueId);
            ViewBag.VisitorId = new SelectList(db.Teams.Where(t => t.LeagueId == view.VisitorLeagueId).OrderBy(t => t.Name), "TeamId", "Name", view.VisitorId);
            
            ViewBag.TournamentGroupId = new SelectList(db.TournamentGroups.Where(tg => tg.TournamentId == date.TournamentId).OrderBy(tg => tg.Name), "TournamentGroupId", "Name", view.TournamentGroupId);
            ViewBag.RefereeId = new SelectList(db.Referees.Where(tg => tg.RefereeId == view.RefereeId).OrderBy(tg => tg.Name), "RefereeId", "Name", view.RefereeId);
            ViewBag.FieldId = new SelectList(db.Fields.Where(tg => tg.FieldId == view.FieldId).OrderBy(tg => tg.Name), "FieldId", "Name", view.FieldId);
            return View(view);
        }

        private Match ToMatch(MatchView view)
        {
            return new Match
            {
                DateId = view.DateId,
                Date = view.Date,
                DateTime = view.DateTime,
                LocalGoals = view.LocalGoals,
                LocalId = view.LocalId,
                Local = view.Local,
                StatusId = view.StatusId,
                TournamentGroupId = view.TournamentGroupId,
                VisitorId = view.VisitorId,
                Visitor = view.Visitor,
                VisitorGoals = view.VisitorGoals,
                MatchId = view.MatchId,
                Predictions = view.Predictions,
                Status = view.Status,
                TournamentGroup = view.TournamentGroup,
                RefereeId = view.RefereeId,
                Referee = view.Referee,
                FieldId = view.FieldId,
                Field = view.Field
            };
        }

        // GET: Dates/Edit/5
        public async Task<ActionResult> DetailsDate(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var date = await db.Dates.FindAsync(id);

            if (date == null)
            {
                return HttpNotFound();
            }

            return View(date);
        }

        // GET: TournamentTeams/Edit/5
        public async Task<ActionResult> DeleteTeam(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var tournamentTeam = await db.TournamentTeams.FindAsync(id);

            if (tournamentTeam == null)
            {
                return HttpNotFound();
            }

            db.TournamentTeams.Remove(tournamentTeam);
            await db.SaveChangesAsync();
            return RedirectToAction(string.Format("DetailsGroup/{0}", tournamentTeam.TournamentGroupId));
        }

        // GET: TournamentTeams/Edit/5
        public async Task<ActionResult> EditTeam(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var tournamentTeam = await db.TournamentTeams.FindAsync(id);

            if (tournamentTeam == null)
            {
                return HttpNotFound();
            }

            ViewBag.LeagueId = new SelectList(db.Leagues.OrderBy(t => t.Name), "LeagueId", "Name", tournamentTeam.Team.LeagueId);
            ViewBag.TeamId = new SelectList(db.Teams.Where(t => t.LeagueId == tournamentTeam.Team.LeagueId).OrderBy(t => t.Name), "TeamId", "Name", tournamentTeam.Team.TeamId);
            var view = ToView(tournamentTeam);
            return View(view);
        }

        private TournamentTeamView ToView(TournamentTeam tournamentTeam)
        {
            return new TournamentTeamView
            {
                AgainstGoals = tournamentTeam.AgainstGoals,
                FavorGoals = tournamentTeam.FavorGoals,
                LeagueId = tournamentTeam.Team.LeagueId,
                MatchesLost = tournamentTeam.MatchesLost,
                MatchesPlayed = tournamentTeam.MatchesPlayed,
                MatchesTied = tournamentTeam.MatchesTied,
                MatchesWon = tournamentTeam.MatchesWon,
                Points = tournamentTeam.Points,
                Position = tournamentTeam.Position,
                Team = tournamentTeam.Team,
                TeamId = tournamentTeam.TeamId,
                TournamentGroup = tournamentTeam.TournamentGroup,
                TournamentGroupId = tournamentTeam.TournamentGroupId,
                TournamentTeamId = tournamentTeam.TournamentTeamId,
            };
        }

        // POST: TournamentTeams/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditTeam(TournamentTeamView view)
        {
            if (ModelState.IsValid)
            {
                var tournamentTeam = ToTournamentTeam(view);
                db.Entry(tournamentTeam).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction(string.Format("DetailsGroup/{0}", tournamentTeam.TournamentGroupId));
            }

            ViewBag.LeagueId = new SelectList(db.Leagues.OrderBy(t => t.Name), "LeagueId", "Name", view.LeagueId);
            ViewBag.TeamId = new SelectList(db.Teams.Where(t => t.LeagueId == view.LeagueId).OrderBy(t => t.Name), "TeamId", "Name", view.TeamId);
            return View(view);
        }

        private TournamentTeam ToTournamentTeam(TournamentTeamView view)
        {
            return new TournamentTeam
            {
                AgainstGoals = view.AgainstGoals,
                FavorGoals = view.FavorGoals,
                MatchesLost = view.MatchesLost,
                MatchesPlayed = view.MatchesPlayed,
                MatchesTied = view.MatchesTied,
                MatchesWon = view.MatchesWon,
                Points = view.Points,
                Position = view.Position,
                Team = view.Team,
                TeamId = view.TeamId,
                TournamentGroup = view.TournamentGroup,
                TournamentGroupId = view.TournamentGroupId,
                TournamentTeamId = view.TournamentTeamId,
            };
        }

        // GET: TournamentTeams/Create
        public async Task<ActionResult> CreateTeam(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var tournamentGroup = await db.TournamentGroups.FindAsync(id);

            if (tournamentGroup == null)
            {
                return HttpNotFound();
            }

            ViewBag.LeagueId = new SelectList(db.Leagues.OrderBy(t => t.Name), "LeagueId", "Name");
            ViewBag.TeamId = new SelectList(db.Teams.Where(t => t.LeagueId == db.Leagues.FirstOrDefault().LeagueId).OrderBy(t => t.Name), "TeamId", "Name");
            var view = new TournamentTeamView { TournamentGroupId = tournamentGroup.TournamentGroupId, };
            return View(view);
        }

        // POST: TournamentTeams/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateTeam(TournamentTeamView view)
        {
            if (ModelState.IsValid)
            {
                var tournamentTeam = ToTournamentTeam(view);
                db.TournamentTeams.Add(tournamentTeam);
                await db.SaveChangesAsync();
                return RedirectToAction(string.Format("DetailsGroup/{0}", tournamentTeam.TournamentGroupId));
            }

            ViewBag.LeagueId = new SelectList(db.Leagues.OrderBy(t => t.Name), "LeagueId", "Name", view.LeagueId);
            ViewBag.TeamId = new SelectList(db.Teams.Where(t => t.LeagueId == view.LeagueId).OrderBy(t => t.Name), "TeamId", "Name", view.TeamId);
            return View(view);
        }

        // GET: Dates/Edit/5
        public async Task<ActionResult> DetailsGroup(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var tournamentGroup = await db.TournamentGroups.FindAsync(id);

            if (tournamentGroup == null)
            {
                return HttpNotFound();
            }

            return View(tournamentGroup);
        }

        // GET: Dates/Create
        public async Task<ActionResult> CreateDate(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var tournament = await db.Tournaments.FindAsync(id);

            if (tournament == null)
            {
                return HttpNotFound();
            }

            var view = new Date { TournamentId = tournament.TournamentId, };
            return View(view);
        }

        // POST: Dates/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateDate(Date date)
        {
            if (ModelState.IsValid)
            {
                db.Dates.Add(date);
                await db.SaveChangesAsync();
                return RedirectToAction(string.Format("Details/{0}", date.TournamentId));
            }

            return View(date);
        }

        // GET: Dates/Edit/5
        public async Task<ActionResult> EditDate(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var date = await db.Dates.FindAsync(id);

            if (date == null)
            {
                return HttpNotFound();
            }

            return View(date);
        }

        // POST: Dates/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditDate(Date date)
        {
            if (ModelState.IsValid)
            {
                db.Entry(date).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction(string.Format("Details/{0}", date.TournamentId));
            }

            return View(date);
        }

        // GET: Dates/Edit/5
        public async Task<ActionResult> DeleteDate(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var date = await db.Dates.FindAsync(id);

            if (date == null)
            {
                return HttpNotFound();
            }

            db.Dates.Remove(date);
            await db.SaveChangesAsync();
            return RedirectToAction(string.Format("Details/{0}", date.TournamentId));
        }

        // GET: TournamentGroups/Details/5
        ////public async Task<ActionResult> DetailsGroup(int? id)
        ////{
        ////    if (id == null)
        ////    {
        ////        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        ////    }

        ////    var tournamentGroup = await db.TournamentGroups.FindAsync(id);

        ////    if (tournamentGroup == null)
        ////    {
        ////        return HttpNotFound();
        ////    }

        ////    return View(tournamentGroup);
        ////}

        // GET: TournamentGroups/Create
        public async Task<ActionResult> CreateGroup(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var tournament = await db.Tournaments.FindAsync(id);

            if (tournament == null)
            {
                return HttpNotFound();
            }

            var view = new TournamentGroup { TournamentId = tournament.TournamentId, };
            return View(view);
        }

        // POST: TournamentGroups/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateGroup(TournamentGroup tournamentGroup)
        {
            if (ModelState.IsValid)
            {
                db.TournamentGroups.Add(tournamentGroup);
                await db.SaveChangesAsync();
                return RedirectToAction(string.Format("Details/{0}", tournamentGroup.TournamentId));
            }

            return View(tournamentGroup);
        }

        // GET: TournamentGroups/Edit/5
        public async Task<ActionResult> EditGroup(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var tournamentGroup = await db.TournamentGroups.FindAsync(id);

            if (tournamentGroup == null)
            {
                return HttpNotFound();
            }

            return View(tournamentGroup);
        }

        // POST: TournamentGroups/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditGroup(TournamentGroup tournamentGroup)
        {
            if (ModelState.IsValid)
            {
                db.Entry(tournamentGroup).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction(string.Format("Details/{0}", tournamentGroup.TournamentId));
            }

            return View(tournamentGroup);
        }

        public async Task<ActionResult> DeleteGroup(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var tournamentGroup = await db.TournamentGroups.FindAsync(id);

            if (tournamentGroup == null)
            {
                return HttpNotFound();
            }

            db.TournamentGroups.Remove(tournamentGroup);
            await db.SaveChangesAsync();
            return RedirectToAction(string.Format("Details/{0}", tournamentGroup.TournamentId));
        }

        // GET: Tournaments
        public async Task<ActionResult> Index()
        {
            return View(await db.Tournaments.ToListAsync());
        }

        // GET: Tournaments/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Tournament tournament = await db.Tournaments.FindAsync(id);
            if (tournament == null)
            {
                return HttpNotFound();
            }
            return View(tournament);
        }

        // GET: Tournaments/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Tournaments/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(TournamentView view)
        {
            if (ModelState.IsValid)
            {
                var pic = string.Empty;
                var folder = "~/Content/Logos";

                if (view.LogoFile != null)
                {
                    pic = FilesHelper.UploadPhoto(view.LogoFile, folder);
                    pic = string.Format("{0}/{1}", folder, pic);
                }

                var tournament = ToTournament(view);
                tournament.Logo = pic;
                db.Tournaments.Add(tournament);
                await db.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            ViewBag.LeagueId = new SelectList(db.Leagues, "LeagueId", "Name", view.TournamentId);
            return View(view);
        }

        private Tournament ToTournament(TournamentView view)
        {
            return new Tournament
            {
                TournamentId = view.TournamentId,
                Name = view.Name,
                Groups = view.Groups,
                Logo = view.Logo,
                Order = view.Order,
                IsActive = view.IsActive,
            };
        }

        // GET: Tournaments/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var tournament = await db.Tournaments.FindAsync(id);

            if (tournament == null)
            {
                return HttpNotFound();
            }

            var view = ToView(tournament);

            return View(view);
        }

        private TournamentView ToView(Tournament tournament)
        {
            return new TournamentView
            {
                TournamentId = tournament.TournamentId,
                Name = tournament.Name,
                Groups = tournament.Groups,
                Logo = tournament.Logo,
                Order = tournament.Order,
                IsActive = tournament.IsActive,
            };
        }

        // POST: Tournaments/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(TournamentView view)
        {
            if (ModelState.IsValid)
            {
                var pic = view.Logo;
                var folder = "~/Content/Logos";

                if (view.LogoFile != null)
                {
                    pic = FilesHelper.UploadPhoto(view.LogoFile, folder);
                    pic = string.Format("{0}/{1}", folder, pic);
                }

                var tournament = ToTournament(view);
                tournament.Logo = pic;
                db.Entry(tournament).State = EntityState.Modified;
                await db.SaveChangesAsync();

                return RedirectToAction("Index");
            }
            return View(view);
        }

        // GET: Tournaments/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Tournament tournament = await db.Tournaments.FindAsync(id);
            if (tournament == null)
            {
                return HttpNotFound();
            }
            return View(tournament);
        }

        // POST: Tournaments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            Tournament tournament = await db.Tournaments.FindAsync(id);
            db.Tournaments.Remove(tournament);
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        // GET: Matches/Delete/5
        public async Task<ActionResult> DeleteMatch(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var match = await db.Matches.FindAsync(id);

            if (match == null)
            {
                return HttpNotFound();
            }

            db.Matches.Remove(match);
            await db.SaveChangesAsync();
            return RedirectToAction(string.Format("DetailsDate/{0}", match.DateId)); ;
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
