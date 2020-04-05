using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Backend.Models;
using Domain;

namespace Backend.Controllers
{
    public class DatesController : Controller
    {
        private DataContextLocal db = new DataContextLocal();

        // GET: Dates
        public async Task<ActionResult> Index()
        {
            var dates = db.Dates.Include(d => d.Tournament);
            return View(await dates.ToListAsync());
        }

        // GET: Dates/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Date date = await db.Dates.FindAsync(id);
            if (date == null)
            {
                return HttpNotFound();
            }
            return View(date);
        }

        // GET: Dates/Create
        public ActionResult Create()
        {
            ViewBag.TournamentId = new SelectList(db.Tournaments, "TournamentId", "Name");
            return View();
        }

        // POST: Dates/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "DateId,Name,TournamentId")] Date date)
        {
            if (ModelState.IsValid)
            {
                db.Dates.Add(date);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            ViewBag.TournamentId = new SelectList(db.Tournaments, "TournamentId", "Name", date.TournamentId);
            return View(date);
        }

        // GET: Dates/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Date date = await db.Dates.FindAsync(id);
            if (date == null)
            {
                return HttpNotFound();
            }
            ViewBag.TournamentId = new SelectList(db.Tournaments, "TournamentId", "Name", date.TournamentId);
            return View(date);
        }

        // POST: Dates/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "DateId,Name,TournamentId")] Date date)
        {
            if (ModelState.IsValid)
            {
                db.Entry(date).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            ViewBag.TournamentId = new SelectList(db.Tournaments, "TournamentId", "Name", date.TournamentId);
            return View(date);
        }

        // GET: Dates/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Date date = await db.Dates.FindAsync(id);
            if (date == null)
            {
                return HttpNotFound();
            }
            return View(date);
        }

        // POST: Dates/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            Date date = await db.Dates.FindAsync(id);
            db.Dates.Remove(date);
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
