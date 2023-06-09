﻿using AspNetCoreProject.Continue.Entities;
using AspNetCoreProject.Continue.Helpers;
using AspNetCoreProject.Continue.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace AspNetCoreProject.Continue.Controllers
{
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public class AccountController : Controller
    {
        private readonly DatabaseContext _databaseContext;
        private readonly IConfiguration _configuration;
        private readonly IHasher _hasher;

        public AccountController(DatabaseContext databaseContext, IConfiguration configuration, IHasher hasher)
        {
            _databaseContext = databaseContext;
            _configuration = configuration;
            _hasher = hasher;
        }

        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                string hashed = _hasher.DoMD5HashedString(model.Password);

                User user = _databaseContext.Users.SingleOrDefault(x => x.Username.ToLower() == model.Username.ToLower() && x.Password == hashed);

                if (user != null)
                {
                    if (user.Locked)
                    {
                        ModelState.AddModelError(nameof(model.Username), "User is locked.");
                        return View(model);
                    }

                    List<Claim> claims = new List<Claim>();
                    claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
                    claims.Add(new Claim(ClaimTypes.Name, user.FullName ?? String.Empty));
                    claims.Add(new Claim(ClaimTypes.Role, user.Role));
                    claims.Add(new Claim("Username", user.Username));


                    ClaimsIdentity identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    ClaimsPrincipal principal = new ClaimsPrincipal(identity);

                    HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError("", "Username or password is incorrect.");
                }


            }

            return View(model);
        }

       

        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (_databaseContext.Users.Any(x => x.Username.ToLower() == model.Username.ToLower()))
                {
                    ModelState.AddModelError(nameof(model.Username), "Username is already exist.");

                    return View(model);
                }

                var user = new User();

                user.Username = model.Username;

                string hashed = _hasher.DoMD5HashedString(model.Password);

                user.Password = hashed;

                _databaseContext.Users.Add(user);

                var affectedRowCount = _databaseContext.SaveChanges();

                if (affectedRowCount == 0)
                {
                    ModelState.AddModelError("", "User cannot be added.");
                }
                else
                {
                    return RedirectToAction(nameof(Login));
                }
            }
            return View(model);
        }
        public IActionResult Profile()
        {
            ProfileInfoLoader();

            return View();
        }

        private void ProfileInfoLoader()
        {
            Guid userid = new Guid(User.FindFirstValue(ClaimTypes.NameIdentifier));
            User user = _databaseContext.Users.SingleOrDefault(x => x.Id == userid);

            ViewData["FullName"] = user.FullName;
          
            ViewData["ProfileImage"] = user.ProfileImageFileName;
            
           
        }

        [HttpPost]
        public IActionResult ProfileChangeFullName([Required][StringLength(50)] string? fullname)
        {
            if (ModelState.IsValid)
            {
                Guid userid = new Guid(User.FindFirstValue(ClaimTypes.NameIdentifier));

                User user = _databaseContext.Users.SingleOrDefault(x => x.Id == userid);

                user.FullName = fullname;
                _databaseContext.SaveChanges();

                return RedirectToAction(nameof(Profile));
            }

            ProfileInfoLoader();
            return View("Profile");
        }

        [HttpPost]
        public IActionResult ProfileChangePassword([Required][MinLength(6)][MaxLength(16)] string? password)
        {
            if (ModelState.IsValid)
            {
                Guid userid = new Guid(User.FindFirstValue(ClaimTypes.NameIdentifier));

                User user = _databaseContext.Users.SingleOrDefault(x => x.Id == userid);

                string hashed = _hasher.DoMD5HashedString(password);

                user.Password = hashed;
                _databaseContext.SaveChanges();

                ViewData["result"] = "PasswordChanged";

            }

            ProfileInfoLoader();
            return View("Profile");
        }

        public IActionResult ProfileChangeImage([Required] IFormFile file)
        {
            if (ModelState.IsValid)
            {
                Guid userid = new Guid(User.FindFirstValue(ClaimTypes.NameIdentifier));
                User user = _databaseContext.Users.SingleOrDefault(x => x.Id == userid);

                //p_guid.jpg
                string fileName = $"p_{userid}.{file.ContentType.Split('/')[1]}";
              
                //string fileName = $"p_{userid}.{file.ContentType.Split('/')[1]}";
                Stream stream = new FileStream($"wwwroot/uploads/{fileName}", FileMode.OpenOrCreate);
                file.CopyTo(stream);
                stream.Close();
                stream.Dispose(); 

                user.ProfileImageFileName = fileName;
                _databaseContext.SaveChanges();

                return RedirectToAction(nameof(Profile));
            }
            ProfileInfoLoader();
            return View("Profile");
        }

        public IActionResult Logout()
        {
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

    }
}
