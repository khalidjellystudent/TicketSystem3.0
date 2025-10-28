using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Data;
using TicketSystem.Models;
using BCrypt.Net;
using System.Data.SqlClient;
using System.IO.Compression;


[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly AppDbContext _db;

    public AdminController(AppDbContext db)
    {
        _db = db;
    }
    public IActionResult Index()
    {
        return View();
    }
    public IActionResult ResetPassword()
    {
        return View();
    }
    [HttpGet]
    public IActionResult ResetPassword(string email)
    {
        if (string.IsNullOrEmpty(email))
            return BadRequest();

        var user = _db.Users.FirstOrDefault(u => u.Email == email);
        if (user == null)
            return NotFound();

        var model = new ResetPasswordViewModel
        {
            Email = user.Email
        };

        return View(model);
    }

    [HttpPost]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = _db.Users.FirstOrDefault(u => u.Email == model.Email);
        if (user == null)
            return NotFound();

        // ✅ Hash the new password
        user.Password = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
        _db.SaveChanges();

        TempData["SuccessMessage"] = $"Password for {user.Email} has been reset.";
        return RedirectToAction("UserList"); // Or wherever your admin list is
    }


    
    public IActionResult UserList()
    {
        var users = _db.Users.ToList();
        return View(users);
    }


    // feedbacks
 
    public IActionResult FeedbackList()
    {
        var feedbacks = _db.Feedbacks
                           .OrderByDescending(f => f.CreatedAt)
                           .ToList();
        return View(feedbacks);
    }

    // delete feed back massage 

    
    [HttpPost]
    public IActionResult MarkAsRead(int id)
    {
        var feedback = _db.Feedbacks.FirstOrDefault(f => f.Id == id);
        if (feedback == null) return NotFound();

        if (!feedback.IsRead)
        {
            feedback.IsRead = true;
            _db.SaveChanges();
        }

        return Ok();
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteFeedback(int id)
    {
        var feedback = _db.Feedbacks.FirstOrDefault(f => f.Id == id);
        if (feedback == null)
        {
            TempData["ErrorMessage"] = "لم يتم العثور على الرسالة.";
            return RedirectToAction("FeedbackList");
        }

        _db.Feedbacks.Remove(feedback);
        _db.SaveChanges();

        TempData["SuccessMessage"] = "تم حذف الرسالة بنجاح.";
        return RedirectToAction("FeedbackList");
    }






    
    public IActionResult OfficerList()
    {
        var officers = _db.Users
                          .Where(u => u.Role == "Officer")
                          .ToList();
        return View(officers);
    }

    public IActionResult OfficerDetails(string email)
    {
        var officer = _db.Users.FirstOrDefault(u => u.Email == email && u.Role == "Officer");
        if (officer == null) return NotFound();

        // Tickets issued by this officer
        var tickets = _db.Tickets
                         .Where(t => t.IssuedBy == officer.Email)
                         .ToList();
        
        var stats = new OfficerStatsViewModel
        {
            Officer = officer,
            TotalTickets = tickets.Count,
            TotalFines = tickets.Sum(t => t.FineAmount),
            TicketsPerMonth = tickets
                .GroupBy(t => new { t.Ticket_Time.Year, t.Ticket_Time.Month })
                .Select(g => new MonthlyStat
                {
                    Month = $"{g.Key.Month}/{g.Key.Year}",
                    Count = g.Count()
                }).ToList(),
            ViolationBreakdown = tickets
                .SelectMany(t => t.Violations.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .GroupBy(v => v.Trim())
                .Select(g => new ViolationStat
                {
                    ViolationType = g.Key,
                    Count = g.Count()
                }).ToList()
        };

        return View(stats);
    }
}
