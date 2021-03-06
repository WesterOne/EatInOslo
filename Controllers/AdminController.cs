using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EatInOslo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Web;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace EatInOslo.Controllers
{
    // [Authorize]
    public class AdminController : Controller
    {

        // ########################################
        //           SETUP & CONSTRUCTOR
        // ########################################
        

        private readonly EatInOsloContext _context;
        private IHostingEnvironment _env;

        public AdminController(EatInOsloContext context, IHostingEnvironment env) 
        {
            _context = context;
            _env = env;
        }

        // ########################################
        //                 ROUTING
        // ########################################

        // Admin login page
        [HttpGet]
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("adminlogin") == "Admin") 
            {
                return RedirectToAction(nameof(Admin));
            }
            return View();
        }

        // Main Admin page
        [HttpGet]
        public async Task<IActionResult> Admin()
        {
            if (HttpContext.Session.GetString("adminlogin") != "Admin") 
            {
                return RedirectToAction(nameof(Index));
            }

            try {
                return View(await _context.Restaurant
                    .Include("Review")
                    .ToListAsync());

            } catch (Exception e) {
                return View(e);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Restaurants()
        {
            if (HttpContext.Session.GetString("adminlogin") != "Admin") 
            {
                return RedirectToAction(nameof(Index));
            }

            try {
                return View(await _context.Restaurant.ToListAsync());
            } catch (Exception e) {
                return View(e);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Reviews()
        {
            if (HttpContext.Session.GetString("adminlogin") != "Admin") 
            {
                return RedirectToAction(nameof(Index));
            }

            try {
                return View(await _context.Review.Include("Restaurant").ToListAsync());
            } catch (Exception e) {
                return View(e);
            }
        }

        // ########################################
        //                 LOGIN
        // ########################################

        // Post request for admin login
        [HttpPost]
        public async Task<IActionResult> Index([Bind("name, password, email")] User user)
        {
            if (ModelState.IsValid) {
                try {
                    User us = await _context.User.SingleOrDefaultAsync(u => u.name == "Admin" && u.password == user.password);

                    if (us != null)
                    {
                        HttpContext.Session.SetString("adminlogin", user.name);
                        return RedirectToAction(nameof(Admin));
                    } else {
                        return View();
                    }

                } catch (Exception e) {
                    return View(e);
                }
            } else {
                return View();
            }
        }

        // #######################################
        //              EDIT/Restaurant 
        // #######################################

        [HttpGet]
        public async Task<IActionResult> EditRestaurant(int? id)
        {
            if (HttpContext.Session.GetString("adminlogin") != "Admin") 
            {
                return RedirectToAction(nameof(Index));
            }

            try {
                return View(await _context.Restaurant
                    .SingleOrDefaultAsync(r => r.ID == id));
            } catch (Exception e) {
                return View(e);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRestaurant(int id, IFormFile file, [Bind("ID, name, type, description, imgurl")] Restaurant restaurant)
        {
            if (ModelState.IsValid)
            {
                try {
                    if (file != null) 
                    {
                        restaurant.imgurl = file.FileName;

                        string path = _env.WebRootPath + "\\image\\Restaurants\\" + file.FileName;
                        using (var stream = new FileStream(path, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }
                    }

                    _context.Restaurant.Update(restaurant);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Restaurants));
                } catch (Exception e) {
                    return View(e);
                }
            } else {
                return View(restaurant);
            }
        }

        // #######################################
        //             DELETE/Restaurant
        // #######################################

        [HttpGet]
        public async Task<IActionResult> DeleteRestaurant(int? id)
        {
            if (HttpContext.Session.GetString("adminlogin") != "Admin") 
            {
                return RedirectToAction(nameof(Index));
            }
            try {
                return View(await _context.Restaurant
                    .SingleOrDefaultAsync(r => r.ID == id));
            } catch (Exception e) {
                return View(e);
            }
        }

        [HttpPost, ActionName("ConfirmDeleteRestaurant")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmDeleteRestaurant(int id)
        {
            _context.Restaurant.Remove(await _context.Restaurant
                .SingleOrDefaultAsync(c => c.ID == id));

            var rev = await _context.Review.Where(_review => _review.RestaurantID == id).ToListAsync();
            foreach (var r in rev)
            {
                 _context.Review.Remove(r);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Restaurants));
        }

        // #######################################
        //             CREATE/Restaurant
        // #######################################

        [HttpGet]
        public IActionResult NewRestaurant()
        {
            if (HttpContext.Session.GetString("adminlogin") != "Admin") 
            {
                return RedirectToAction(nameof(Index));
            }

            return View();
        }

        // Save Restaurant with Image upload
        [HttpPost]
        public async Task<IActionResult> NewRestaurant(IFormFile file, [Bind("ID,name,type,description,imgurl")] Restaurant restaurant) 
        {
            if (ModelState.IsValid)
            {
                if (file == null || file.Length == 0)
                {
                    return Content("file not selected");
                }

                try {
                    string path = _env.WebRootPath + "\\image\\Restaurants\\" + file.FileName;
                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    restaurant.imgurl = file.FileName;
                    _context.Restaurant.Add(restaurant);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Restaurants));
                } catch (Exception e) {
                    return View(e);
                }
            } else {
                return View(restaurant);
            }
        }

        // #######################################
        //             EDIT/Review
        // #######################################

        [HttpGet]
        public async Task<IActionResult> EditReview(int? id)
        {
            if (HttpContext.Session.GetString("adminlogin") != "Admin") 
            {
                return RedirectToAction(nameof(Index));
            }

            try {
                return View(await _context.Review.Include("User").SingleOrDefaultAsync(r => r.ID == id));
            } catch (Exception e) {
                return View(e);
            }
            
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditReview(int id, [Bind("ID, text, title, UserID, RestaurantID")] Review review)
        {
            if (ModelState.IsValid)
            {
                try 
                {
                    _context.Review.Update(review);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Admin));
                } 
                    catch (Exception ex)
                {
                    return View(ex);
                }

            } else {
                return View(review);
            }
        }

        // #######################################
        //             Delete/Review
        // #######################################

        public async Task<IActionResult> DeleteReview(int? id)
        {
            if (HttpContext.Session.GetString("adminlogin") != "Admin") 
            {
                return RedirectToAction(nameof(Index));
            }

            return View(await _context.Review.SingleOrDefaultAsync(r => r.ID == id));
        }

        [HttpPost, ActionName("ConfirmDeleteReview")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmDeleteReview(int id)
        {
            try {
                _context.Review.Remove(await _context.Review.SingleOrDefaultAsync(r => r.ID == id));
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Admin));
            } catch (Exception e) {
                return View(e);
            }
        }

    }
}