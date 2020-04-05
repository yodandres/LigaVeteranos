using Backend.Classes;
using Backend.Helpers;
using Backend.Models;
using Domain;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Backend.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private DataContextLocal db = new DataContextLocal();

        // GET: Users
        public async Task<ActionResult> Index()
        {
            var users = db.Users.Include(u => u.FavoriteTeam).Include(u => u.UserType);
            return View(await users.ToListAsync());
        }

        // GET: Users/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            User user = await db.Users.FindAsync(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }
                
        public ActionResult Create()
        {
            ViewBag.FavoriteLeagueId = new SelectList(db.Leagues.OrderBy(l => l.Name), "LeagueId", "Name");
            ViewBag.FavoriteTeamId = new SelectList(db.Teams.Where(t => t.LeagueId == db.Leagues.FirstOrDefault().LeagueId).OrderBy(l => l.Name), "TeamId", "Name");
            ViewBag.UserTypeId = new SelectList(db.UserTypes.OrderBy(ut => ut.Name), "UserTypeId", "Name");
            return View();
        }
               
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(UserView view)
        {
            if (ModelState.IsValid)
            {
                var pic = string.Empty;
                var folder = "~/Content/Users";

                if (view.PictureFile != null)
                {
                    pic = FilesHelper.UploadPhoto(view.PictureFile, folder);
                    pic = string.Format("{0}/{1}", folder, pic);
                }

                var user = ToUser(view);
                user.Picture = pic;
                db.Users.Add(user);
                await db.SaveChangesAsync();
                UsersHelper.CreateUserASP(view.Email, "User", view.Password);
                return RedirectToAction("Index");
            }

            ViewBag.FavoriteLeagueId = new SelectList(db.Leagues.OrderBy(l => l.Name), "LeagueId", "Name", view.FavoriteLeagueId);
            ViewBag.FavoriteTeamId = new SelectList(db.Teams.Where(t => t.LeagueId == view.FavoriteLeagueId).OrderBy(t => t.Name), "TeamId", "Name", view.FavoriteTeamId);
            ViewBag.UserTypeId = new SelectList(db.UserTypes.OrderBy(ut => ut.Name), "UserTypeId", "Name", view.UserTypeId);
            return View(view);
        }

        private User ToUser(UserView view)
        {
            return new User()
            {
                FirstName = view.FirstName,
                LastName = view.LastName,
                NickName = view.NickName,
                Email = view.Email,
                FavoriteTeamId = view.FavoriteTeamId,
                FavoriteTeam = view.FavoriteTeam,
                Picture = view.Picture,
                Points = view.Points,
                UserId = view.UserId,
                UserType = view.UserType,
                UserTypeId = view.UserTypeId,
                Predictions = view.Predictions,
            };
        }
        
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var user = await db.Users.FindAsync(id);

            if (user == null)
            {
                return HttpNotFound();
            }

            ViewBag.FavoriteTeamId = new SelectList(db.Teams, "TeamId", "Name", user.FavoriteTeamId);
            ViewBag.UserTypeId = new SelectList(db.UserTypes, "UserTypeId", "Name", user.UserTypeId);
            var view = ToView(user);
            return View(view);
        }

        private UserView ToView(User user)
        {
            return new UserView
            {
                UserId = user.UserId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FavoriteLeagueId = user.FavoriteTeamId,
                FavoriteTeam = user.FavoriteTeam,
                Email = user.Email,
                NickName = user.NickName,
                Picture = user.Picture,
                Points = user.Points,
                UserType = user.UserType,
                UserTypeId = user.UserTypeId,
            };
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(UserView userView)
        {
            ////if (ModelState.IsValid)
            ////{
                var pic = userView.Picture;
                var folder = "~/Content/Users";

                if (userView.PictureFile != null)
                {
                    pic = FilesHelper.UploadPhoto(userView.PictureFile, folder);
                    pic = string.Format("{0}/{1}", folder, pic);
                }

                var user = ToUser(userView);
                user.Picture = pic;
                db.Entry(user).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            ////}

            ////ViewBag.FavoriteTeamId = new SelectList(db.Teams, "TeamId", "Name", userView.FavoriteTeamId);
            ////ViewBag.UserTypeId = new SelectList(db.UserTypes, "UserTypeId", "Name", userView.UserTypeId);
            ////return View(userView);
        }
        
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            User user = await db.Users.FindAsync(id);

            if (user == null)
            {
                return HttpNotFound();
            }

            ////db.Users.Remove(user);
            ////await db.SaveChangesAsync();
            return View(user);
        }
        
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            User user = await db.Users.FindAsync(id);
            db.Users.Remove(user);
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
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
