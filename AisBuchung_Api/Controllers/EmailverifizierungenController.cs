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
    [Route("emailverifizierungen")]
    [ApiController]
    public class EmailverifizierungenController : ControllerBase
    {
        private readonly EmailverifizierungenModel model;
        private readonly AuthModel auth;

        public EmailverifizierungenController()
        {
            model = new EmailverifizierungenModel();
            auth = new AuthModel();
        }

        [HttpGet]
        public ActionResult<IEnumerable<string>> GetAllCodes(LoginPost loginPost)
        {
            if (!auth.CheckIfDebugPermissions(loginPost))
            {
                return Unauthorized();
            }

            var result = model.GetAllCodes();
            return Content(result, "application/json");
        }

        [HttpPost("bereinigen")]
        public ActionResult<IEnumerable<string>> WipeUnnecessaryCodes(LoginPost loginPost)
        {
            if (!auth.CheckIfDebugPermissions(loginPost))
            {
                return Unauthorized();
            }

            model.WipeUnnecessaryData();
            return Ok();
        }

        [HttpPost("{code}/bestätigen")]
        public ActionResult<IEnumerable<string>> SubmitCode(string code)
        {
            var result = model.ProcessVerification(code);
            if (result)
            {
                return Ok();
            }
            else
            {
                return BadRequest();
            }
        }

        [HttpPost("{code}/versenden")]
        public ActionResult<IEnumerable<string>> SendCodeEmail(LoginPost loginPost, string code)
        {
            if (!auth.CheckIfDebugPermissions(loginPost))
            {
                return Unauthorized();
            }

            return BadRequest();
        }

        [HttpDelete("{code}")]
        public ActionResult<IEnumerable<string>> SubmitCode(LoginPost loginPost, string code)
        {
            if (!auth.CheckIfDebugPermissions(loginPost))
            {
                return Unauthorized();
            }

            var id = model.GetVerificationCodeId(code);

            if (id == -1)
            {
                return NotFound();
            }

            var result = model.DeleteVerificationCode(id);
            if (result)
            {
                return Ok();
            }
            else
            {
                return BadRequest();
            }
        }
    }
}
