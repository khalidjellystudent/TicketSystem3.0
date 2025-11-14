using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TicketSystem.Data;
using TicketSystem.Models;

namespace TicketSystem.Controllers
{
     [Authorize(Roles = "Office")]
    public class OfficeController : Controller
    {
        private readonly AppDbContext _db;
        public OfficeController(AppDbContext context)
        {
            _db = context;
        }
        
        public IActionResult Index()
        {
            return View();
        }
        
        
        public IActionResult ScanPlate()
        {
            return View();
        }
        public IActionResult Reports()
        {
            return View();

        }


        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }


        [HttpGet]
        public async Task<IActionResult> Process()
        {
            // Fetch only tickets whose status is "UnderProcess"
            var tickets = await _db.Tickets
                .Where(t => t.Status == "UnderProcess")
                .OrderByDescending(t => t.Ticket_Time)
                .ToListAsync();

            return View(tickets);
        }

        [HttpPost]
        public IActionResult DeleteTicket(int Ticket_Id)
        {
            var ticket = _db.Tickets.FirstOrDefault(t => t.Ticket_Id == Ticket_Id);
            if (ticket != null)
            {
                _db.Tickets.Remove(ticket);
                _db.SaveChanges();
            }

            return RedirectToAction("Process"); // Replace with your actual view name
        }





        [HttpPost]
        public async Task<IActionResult> Register(User model)
        {
            if (ModelState.IsValid)
            {
                if (_db.Users.Any(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "This email is already registered.");
                    return View(model);
                }

                try
                {
                    // ✅ Hash the password before saving
                    model.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);

                    _db.Users.Add(model);
                    await _db.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Registration successful! Please log in.";
                    return RedirectToAction("Login", "Home"); // Redirect to login instead of Office
                }
                catch (Exception)
                {
                    // In real apps, log the error
                    ModelState.AddModelError("", "An error occurred. Please try again.");
                    return View(model);
                }
            }

            // Return with validation errors
            return View(model);
        }



        [HttpGet]
        public IActionResult SearchTickets(string plateNumber)
        {
            // Search case-insensitive and trim whitespace
            var normalizedPlate = plateNumber?.Trim().ToUpper();

            var tickets = _db.Tickets
                .Include(t => t.Users) // Load related user data
                .Where(t => t.Plate_Number.ToUpper() == normalizedPlate)
                .OrderByDescending(t => t.Ticket_Time)
                .ToList();

            ViewBag.SearchQuery = plateNumber;
            return View("TicketResults", tickets);
        }

        
        


        [HttpGet]
        public IActionResult DetailedTicket(int id)
        {
            var ticket = _db.Tickets
                .Include(t => t.Users)
                .FirstOrDefault(t => t.Ticket_Id == id);

            if (ticket == null)
            {
                return NotFound();
            }

            return View(ticket); // Passing single ticket, not a list
        }

        [HttpPost]
        public IActionResult DenieRevoke_Ticket(int ticketId)
        {
            var ticket = _db.Tickets.Find(ticketId);
            if (ticket != null)
            {
                ticket.Status = "Pending";
                _db.SaveChanges();
                return RedirectToAction("Index", new { message = "After reviewing the Ticket The is Appeal Revoked" });
            }
            return View("Error");
        }


        public IActionResult ProcessPayment(int ticketId)
        {
            var ticket = _db.Tickets.Find(ticketId);
            if (ticket != null)
            {
                ticket.Status = "Paid";
                _db.SaveChanges();
                return RedirectToAction("Index", new { message = "Payment successful!" });
            }
            return View("Error");
        }




    }
}
