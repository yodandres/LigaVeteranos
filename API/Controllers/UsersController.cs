using System;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using API.Classes;
using Domain;
using Newtonsoft.Json.Linq;
using System.IO;
using Microsoft.AspNet.Identity;
using API.Models;
using Microsoft.AspNet.Identity.EntityFramework;

namespace API.Controllers
{
    [RoutePrefix("api/Users")]
    public class UsersController : ApiController
    {
        private DataContext db = new DataContext();

        [HttpPost]
        [Route("PasswordRecovery")]
        public async Task<IHttpActionResult> PasswordRecovery(JObject form)
        {
            try
            {
                var email = string.Empty;
                dynamic jsonObject = form;

                try
                {
                    email = jsonObject.Email.Value;
                }
                catch
                {
                    return BadRequest("Incorrect call");
                }

                var user = await db.Users
                    .Where(u => u.Email.ToLower() == email.ToLower())
                    .FirstOrDefaultAsync();
                if (user == null)
                {
                    return NotFound();
                }

                var userContext = new ApplicationDbContext();
                var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(userContext));
                var userASP = userManager.FindByEmail(email);
                if (userASP == null)
                {
                    return NotFound();
                }

                var random = new Random();
                var newPassword = string.Format("{0}", random.Next(100000, 999999));
                var response1 = userManager.RemovePassword(userASP.Id);
                var response2 = await userManager.AddPasswordAsync(userASP.Id, newPassword);
                if (response2.Succeeded)
                {
                    var subject = "Liga Premier App- Password Recovery";
                    var body = string.Format(@"
            <h1>Liga Premier App - Password Recovery</h1>
            <p>Your new password is: <strong>{0}</strong></p>
            <p>Please, don't forget change it for one easy remember for you.",
                        newPassword);

                    await MailHelper.SendMail(email, subject, body);
                    return Ok(true);
                }

                return BadRequest("The password can't be changed.");

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpPost]
        [Route("GetUserByEmail")]
        public async Task<IHttpActionResult> GetUserByEmail(JObject form)
        {
            var email = string.Empty;
            dynamic jsonObject = form;

            try
            {
                email = jsonObject.Email.Value;
            }
            catch
            {
                return BadRequest("Incorrect call");
            }

            var user = await db.Users.Where(u => u.Email.ToLower() == email.ToLower()).FirstOrDefaultAsync();
            if (user == null)
            {
                return NotFound();
            }

            var userResponse = ToUserResponse(user);
            return Ok(userResponse);
        }


        public IQueryable<User> GetUsers()
        {
            return db.Users;
        }
                
        [ResponseType(typeof(User))]
        public async Task<IHttpActionResult> GetUser(int id)
        {
            var user = await db.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            var userResponse = ToUserResponse(user);
            return Ok(userResponse);
        }

        private UserResponse ToUserResponse(User user)
        {
            return new UserResponse
            {
                Email = user.Email,
                FavoriteTeam = user.FavoriteTeam,
                FavoriteTeamId = user.FavoriteTeamId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                NickName = user.NickName,
                UserId = user.UserId,
                UserType = user.UserType,
                UserTypeId = user.UserTypeId,
                GroupUsers = user.GroupUsers.ToList(),
                Predictions = user.Predictions.ToList(),
                UserGroups = user.UserGroups.ToList(),
                Points = user.Points,
                Picture = user.Picture,
            };
        }

        // PUT: api/Users/5
        [ResponseType(typeof(void))]
        public async Task<IHttpActionResult> PutUser(int id, User user)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != user.UserId)
            {
                return BadRequest();
            }

            db.Entry(user).State = EntityState.Modified;

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST: api/Users
        [ResponseType(typeof(User))]
        public async Task<IHttpActionResult> PostUser(UserRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (request.ImageArray != null && request.ImageArray.Length > 0)
            {
                var stream = new MemoryStream(request.ImageArray);
                var guid = Guid.NewGuid().ToString();
                var file = string.Format("{0}.jpg", guid);
                var folder = "~/Content/Users";
                var fullPath = string.Format("{0}/{1}", folder, file);
                var response = FilesHelper.UploadPhoto(stream, folder, file);

                if (!response)
                {
                    request.Picture = fullPath;
                }
            }

            var user = ToUser(request);
            db.Users.Add(user);
            await db.SaveChangesAsync();
            CreateUserASP(request.Email, "User", request.Password);

            return CreatedAtRoute("DefaultApi", new { id = user.UserId }, user);
        }

        private User ToUser(UserRequest request)
        {
            return new User
            {
                Email = request.Email,
                FavoriteTeamId = request.FavoriteTeamId,
                FirstName = request.FirstName,
                LastName = request.LastName,
                NickName = request.NickName,
                Picture = request.Picture,
                Points = request.Points,
                UserTypeId = request.UserTypeId
            };
        }

        public static void CreateUserASP(string email, string roleName, string password)
        {
            var userContext = new ApplicationDbContext();
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(userContext));

            var userASP = new ApplicationUser
            {
                Email = email,
                UserName = email,
            };

            var result = userManager.Create(userASP, password);
            if (result.Succeeded)
            {
                userManager.AddToRole(userASP.Id, roleName);
            }
        }

        // DELETE: api/Users/5
        [ResponseType(typeof(User))]
        public async Task<IHttpActionResult> DeleteUser(int id)
        {
            User user = await db.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            db.Users.Remove(user);
            await db.SaveChangesAsync();

            return Ok(user);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool UserExists(int id)
        {
            return db.Users.Count(e => e.UserId == id) > 0;
        }
    }
}