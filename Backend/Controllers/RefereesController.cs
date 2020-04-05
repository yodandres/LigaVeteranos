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
    [Authorize]
    public class RefereesController : Controller
    {
        private DataContextLocal db = new DataContextLocal();

        // GET: Referees
        public async Task<ActionResult> Index()
        {
            return View(await db.Referees.ToListAsync());
        }

        // GET: Referees/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Referee referee = await db.Referees.FindAsync(id);
            if (referee == null)
            {
                return HttpNotFound();
            }
            return View(referee);
        }

        // GET: Referees/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Referees/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "RefereeId,Name")] Referee referee)
        {
            if (ModelState.IsValid)
            {
                db.Referees.Add(referee);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            return View(referee);
        }

        // GET: Referees/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Referee referee = await db.Referees.FindAsync(id);
            if (referee == null)
            {
                return HttpNotFound();
            }
            return View(referee);
        }

        // POST: Referees/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "RefereeId,Name")] Referee referee)
        {
            if (ModelState.IsValid)
            {
                db.Entry(referee).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(referee);
        }

        // GET: Referees/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Referee referee = await db.Referees.FindAsync(id);
            if (referee == null)
            {
                return HttpNotFound();
            }
            return View(referee);
        }

        // POST: Referees/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            Referee referee = await db.Referees.FindAsync(id);
            db.Referees.Remove(referee);
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
