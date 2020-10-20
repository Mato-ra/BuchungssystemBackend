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
    [Route("nutzer")]
    [ApiController]
    public class NutzerController : ControllerBase
    {
        private readonly NutzerModel model;
        private readonly AuthModel auth;

        public NutzerController()
        {
            model = new NutzerModel();
            auth = new AuthModel();
        }

        [HttpGet]
        public ActionResult<IEnumerable<string>> GetAllUsers(LoginPost loginPost)
        {
            if (!auth.CheckIfDebugPermissions(loginPost))
            {
                return Unauthorized();
            }

            var query = Request.QueryString.ToUriComponent();
            query = System.Web.HttpUtility.UrlDecode(query);
            var result = model.GetUsers(query);
            return Content(result, "application/json");
        }

        [HttpGet("{id}")]
        public ActionResult<IEnumerable<string>> GetUser(LoginPost loginPost, long id)
        {
            if (!auth.CheckIfDebugPermissions(loginPost))
            {
                return Unauthorized();
            }

            var result = model.GetUser(id);
            if (result == null)
            {
                return NotFound();
            }
            return Content(result, "application/json");
        }

        [HttpPost("{id}/verifizieren")]
        public ActionResult<IEnumerable<string>> VerifyUser(LoginPost loginPost, long id)
        {
            if (!auth.CheckIfDebugPermissions(loginPost))
            {
                return Unauthorized();
            }

            var result = model.VerifyUser(id);
            if (result > 0)
            {
                return NoContent();
            }
            else
            {
                return Conflict();
            }
        }

        [HttpPost("bereinigen")]
        public ActionResult<IEnumerable<string>> CleanUpUserData(LoginPost loginPost)
        {
            if (!auth.CheckIfDebugPermissions(loginPost))
            {
                return Unauthorized();
            }

            var result = model.WipeUnnecessaryData();
            if (result)
            {
                return NoContent();
            }
            else
            {
                return BadRequest();
            }
        }

        [HttpPut("{id}")]
        public ActionResult<IEnumerable<string>> PutUser(long id, UserPost userPost)
        {
            if (!auth.CheckIfDebugPermissions(userPost))
            {
                return Unauthorized();
            }

            //TODO UserPost überprüfen

            var result = model.PutUser(id, userPost);
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
