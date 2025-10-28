using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using TicketSystem.Data;
using TicketSystem.Models;


namespace TicketSystem.Controllers
{
    [Authorize(Roles ="Officer")]
    public class OfficerController : Controller
    {
        AppDbContext _db;
        
        

        public OfficerController(AppDbContext context)
        {
            _db = context;
        }
        public IActionResult Index()
        {
            var currentUserEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var today = DateTime.Today;
            var currentMonth = DateTime.Now.Month;

            var totalTickets = _db.Tickets
                .Where(t => t.IssuedBy == currentUserEmail)
                .Count();

            var paidTickets = _db.Tickets
                .Where(t => t.IssuedBy == currentUserEmail && t.Status == "Paid")
                .Count();

            var paidPercentage = totalTickets > 0 ?
                (paidTickets * 100.0 / totalTickets) : 0;

            var dashboardData = new OfficerDashboardViewModel
            {
                TicketsToday = _db.Tickets
                    .Where(t => t.IssuedBy == currentUserEmail && t.Ticket_Time.Date == today)
                    .Count(),

                PaidTickets = paidTickets,

                PendingTickets = _db.Tickets
                    .Where(t => t.IssuedBy == currentUserEmail && t.Status == "Pending")
                    .Count(),

                MonthlyTickets = _db.Tickets
                    .Where(t => t.IssuedBy == currentUserEmail && t.Ticket_Time.Month == currentMonth)
                    .Count(),

                PaidPercentage = paidPercentage,
                TotalTickets = totalTickets,

                RecentTickets = _db.Tickets
                    .Where(t => t.IssuedBy == currentUserEmail)
                    .OrderByDescending(t => t.Ticket_Time)
                    .Take(10)
                    .ToList()
            };

            return View(dashboardData);
        }
        public IActionResult ScanPlate()
        {
            return View();
        }
        public IActionResult IssueTicket()
        {
            return View();
        }

        [HttpPost]
        public IActionResult WritingTicket(Ticket u, string[] Violation)
        {      
            // set who issued the ticket
            u.IssuedBy = User.FindFirst(ClaimTypes.Email)?.Value;

            // join selected violations into one comma-delimited string
            u.Violations = (Violation != null)
                ? string.Join(",", Violation)
                : string.Empty;

            // serialize and pass to confirmation via TempData
            TempData["PendingTicket"] = JsonSerializer.Serialize(u);
            return RedirectToAction("ConfirmTicket");
        }




        // New action to handle the confirmation page
        public IActionResult ConfirmTicket()
        {
            if (TempData["PendingTicket"] == null)
            {
                return RedirectToAction("IssueTicket");
            }

            var ticketJson = TempData["PendingTicket"].ToString();
            var ticket = JsonSerializer.Deserialize<Ticket>(ticketJson);

            // Store again in TempData for the final submission
            TempData["PendingTicket"] = ticketJson;

            // Check for error message
            if (TempData["ErrorMessage"] != null)
            {
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
            }

            return View(ticket);
        }

        // New action to handle the final submission
        [HttpPost]
        public IActionResult ConfirmTicket(bool confirm)
        {
            if (confirm && TempData["PendingTicket"] != null)
            {
                var ticketJson = TempData["PendingTicket"].ToString();
                var ticket = JsonSerializer.Deserialize<Ticket>(ticketJson);

                // ✅ Check for at least one violation
                if (string.IsNullOrWhiteSpace(ticket.Violations))
                {
                    TempData["PendingTicket"] = ticketJson; // keep data
                    TempData["ErrorMessage"] = "يجب اختيار مخالفة واحدة على الأقل قبل إصدار التذكرة.";
                    return RedirectToAction("ConfirmTicket");
                }

                // ✅ Check if email exists
                if (!string.IsNullOrEmpty(ticket.Email))
                {
                    var userExists = _db.Users.Any(u => u.Email == ticket.Email);
                    if (!userExists)
                    {
                        TempData["PendingTicket"] = ticketJson;
                        TempData["ErrorMessage"] = "This account does not exist. Please check the email address.";
                        return RedirectToAction("ConfirmTicket");
                    }
                }

                // ✅ Save to DB
                _db.Tickets.Add(ticket);
                _db.SaveChanges();

                TempData["SuccessMessage"] = "Ticket issued successfully!";
                return RedirectToAction("TicketIssued", new { id = ticket.Ticket_Id });
            }
            else
            {
                return RedirectToAction("IssueTicket");
            }
        }



        [HttpPost]
        public IActionResult DeleteTicket(int ticket_Id)
        {
            var ticket = _db.Tickets.FirstOrDefault(t => t.Ticket_Id == ticket_Id);
            if (ticket != null)
            {
                _db.Tickets.Remove(ticket);
                _db.SaveChanges();
            }

            return RedirectToAction("Process"); // Replace with your actual view name
        }


        // Success page after ticket is issued
        public IActionResult TicketIssued(int id)
        {
            var ticket = _db.Tickets.Find(id);
            if (ticket == null)
            {
                return NotFound();
            }

            ViewData["SuccessMessage"] = TempData["SuccessMessage"];
            return View(ticket);
        }


        // to her

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
        ////////// ************************************under development i am tinking about removing it 90% ********************
        ///
        [HttpPost]
        public IActionResult UpdateNotes(int ticketId, string notes)
        {
            var ticket = _db.Tickets.Find(ticketId);
            if (ticket == null) return NotFound();

            ticket.Notes = notes;
            _db.SaveChanges();

            TempData["Message"] = "Notes updated successfully";
            return RedirectToAction("DetailedTicket", new { id = ticketId });
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


        // editing section
        [HttpPost]
        public IActionResult EditTicket()
        {
            // Keep TempData alive for another request
            TempData.Keep("PendingTicket");

            var json = TempData["PendingTicket"] as string;
            if (string.IsNullOrEmpty(json))
            {
                return RedirectToAction("IssueTicket"); // fallback if data is missing
            }

            var ticket = JsonSerializer.Deserialize<Ticket>(json);
            return View("IssueTicket", ticket); // pass the ticket back to the form
        }


    }

}
