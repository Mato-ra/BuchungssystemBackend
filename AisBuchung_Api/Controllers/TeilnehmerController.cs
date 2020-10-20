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
    [Route("teilnehmer")]
    [ApiController]
    public class TeilnehmerController : ControllerBase
    {
        private readonly TeilnehmerModel model;
        private readonly AuthModel auth;

        public TeilnehmerController()
        {
            model = new TeilnehmerModel();
            auth = new AuthModel();
        }

        [HttpGet]
        public ActionResult<IEnumerable<string>> GetAllParticipants(LoginPost loginPost)
        {
            if (!auth.CheckIfDebugPermissions(loginPost))
            {
                return Unauthorized();
            }

            return Content(model.GetParticipants(), "application/json");
        }

        [HttpGet("{eventUid}")]
        public ActionResult<IEnumerable<string>> GetParticipants(LoginPost loginPost, string eventUid)
        {
            var calId = new VeranstaltungenModel().GetCalendarId(eventUid);

            if(calId == -1)
            {
                return NotFound();
            }

            if (!auth.CheckIfCalendarPermissions(loginPost, calId))
            {
                return Unauthorized();
            }

            return Content(model.GetParticipants(eventUid), "application/json");
        }

        [HttpDelete("{eventUid}/{participationId}")]
        public ActionResult<IEnumerable<string>> DeleteParticipant(LoginPost loginPost, string eventUid, long participationId)
        {
            var v = new VeranstaltungenModel();
            var calId = v.GetCalendarId(eventUid);
            var eventId = new BuchungenModel().GetEventId(eventUid);

            if (calId == -1)
            {
                return NotFound();
            }

            if (!auth.CheckIfCalendarPermissions(loginPost, calId))
            {
                return Unauthorized();
            }

            if (model.DeleteParticipant(new Dictionary<string, string> { { "veranstaltung", eventId },{ "nutzer", participationId.ToString() } }))
            {
                return Ok();
            }
            else
            {
                return NotFound();
            }
        }

        [HttpPost("bereinigen")]
        public ActionResult<IEnumerable<string>> CleanUpParticipantData(LoginPost loginPost)
        {
            if (!auth.CheckIfDebugPermissions(loginPost))
            {
                return BadRequest();
            }
            model.WipeUnnecessaryData();
            return NoContent();
        }

    }
}
