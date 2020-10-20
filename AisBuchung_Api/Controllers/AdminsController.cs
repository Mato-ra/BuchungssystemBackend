using System;
using System.Web;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore;
using AisBuchung_Api.Models;

namespace AisBuchung_Api.Controllers
{
    [Route("admins")]
    [ApiController]
    public class AdminsController : ControllerBase
    {
        private readonly AdminsModel model;
        private readonly AuthModel auth;

        public AdminsController()
        {
            model = new AdminsModel();
            auth = new AuthModel();
        }

        [HttpGet]
        public ActionResult<IEnumerable<string>> GetAllAdmins(LoginPost loginPost)
        {
            if (!auth.CheckIfOrganizerPermissions(loginPost))
            {
                return Unauthorized();
            }

            var query = Request.QueryString.ToUriComponent();
            query = System.Web.HttpUtility.UrlDecode(query);
            var result = model.GetAdmins(query);
            return Content(result, "application/json");
        }

        [HttpGet("{id}")]
        public ActionResult<IEnumerable<string>> GetAdmin(LoginPost loginPost, long id)
        {
            if (!auth.CheckIfOrganizerPermissions(loginPost))
            {
                return Unauthorized();
            }

            var result = model.GetAdmin(id);
            if (result == null)
            {
                return NotFound();
            }
            return Content(result, "application/json");
        }

        [HttpPost]
        public ActionResult<IEnumerable<string>> PostAdmin(AdminsPost adminsPost)
        {
            if (!auth.CheckIfAdminPermissions(adminsPost))
            {
                return Unauthorized();
            }

            var id = adminsPost.veranstalter;
            var result = model.PostAdmin(id);
            if (result)
            {
                var path = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.Value}/admins/{id}";
                return Created(path, null);
            }
            else
            {
                return BadRequest();
            }
        }

        [HttpDelete("{id}")]
        public ActionResult<IEnumerable<string>> DeleteAdmin(LoginPost loginPost, long id)
        {
            if (!auth.CheckIfAdminPermissions(loginPost))
            {
                return Unauthorized();
            }

            var result = model.DeleteAdmin(id);
            if (result)
            {
                return NoContent();
            }
            else
            {
                return NotFound();
            }
            
        }
    }
    
    
}
