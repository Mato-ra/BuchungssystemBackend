using System;
using System.Web;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore;
using AisBuchung_Api.Models;
using Microsoft.AspNetCore.Http;

namespace AisBuchung_Api.Controllers
{
    [Route("daten")]
    [ApiController]
    public class DatenController : ControllerBase
    {
        private readonly AuthModel auth;
        private readonly DatenModel model;

        public DatenController()
        {
            model = new DatenModel();
            auth = new AuthModel();
        }

        [HttpPost("bereinigen")]
        public ActionResult<IEnumerable<string>> WipeUnnecessaryData(LoginPost loginPost)
        {
            if (!auth.CheckIfDebugPermissions(loginPost))
            {
                return Unauthorized();
            }

            model.WipeUnnecessaryData();

            return Ok();
        }

        [HttpPost("leeren")]
        public ActionResult<IEnumerable<string>> ClearData(LoginPost loginPost)
        {
            if (!auth.CheckIfDebugPermissions(loginPost))
            {
                return Unauthorized();
            }

            if (!model.SaveData())
            {
                return Conflict();
            };

            model.ClearData();

            return Ok();
        }

        [HttpPost("sichern")]
        public ActionResult<IEnumerable<string>> SaveData(LoginPost loginPost)
        {
            if (!auth.CheckIfDebugPermissions(loginPost))
            {
                return Unauthorized();
            }

            if (!model.SaveData())
            {
                return Conflict();
            };

            return Ok();
        }
    }
}
