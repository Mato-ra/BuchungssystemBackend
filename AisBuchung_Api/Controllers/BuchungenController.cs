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
    [Route("buchungen")]
    [ApiController]
    public class BuchungenController : ControllerBase
    {
        private readonly BuchungenModel model;
        private readonly AuthModel auth;

        public BuchungenController()
        {
            model = new BuchungenModel();
            auth = new AuthModel();
        }

        [HttpGet("{eventUid}")]
        public ActionResult<IEnumerable<string>> GetBookings(LoginPost loginPost, string eventUid)
        {

            var eventId = new VeranstaltungenModel().GetEvent(eventUid);
            if (eventId == null)
            {
                return NotFound();
            }

            var calendarId = new VeranstaltungenModel().GetCalendarId(eventUid);
            if (!auth.CheckIfCalendarPermissions(loginPost, calendarId))
            {
                return Unauthorized();
            }

            var query = Request.QueryString.ToUriComponent();
            query = System.Web.HttpUtility.UrlDecode(query);

            var result = model.GetBookings(eventUid, query);
            if (result != null)
            {
                return Content(result, "application/json");
            }
            else
            {
                return NotFound();
            }
        }

        [HttpPost("{eventUid}")]
        public ActionResult<IEnumerable<string>> PostBooking(string eventUid, BookingPost bookingPost)
        {
            if (model.GetEventId(eventUid) == null)
            {
                return NotFound();
            }

            var result = model.PostBooking(bookingPost, eventUid);
            if (result)
            {
                return Ok();
            }
            else
            {
                return BadRequest();
            }
        }

        [HttpPost("{eventUid}/verarbeiten")]
        public ActionResult<IEnumerable<string>> ProcessBookings(LoginPost loginPost, string eventUid)
        {
            if (!auth.CheckIfDebugPermissions(loginPost))
            {
                return Unauthorized();
            }

            if (new VeranstaltungenModel().GetCalendarId(eventUid) == -1)
            {
                return NotFound();
            }

            model.ProcessBookings(eventUid);
            return Ok();
        }

        [HttpPost("{eventUid}/{bookingId}/verarbeiten")]
        public ActionResult<IEnumerable<string>> ProcessBookings(LoginPost loginPost, string eventUid, long bookingId)
        {
            if (!auth.CheckIfDebugPermissions(loginPost))
            {
                return Unauthorized();
            }

            if (new VeranstaltungenModel().GetCalendarId(eventUid) == -1)
            {
                return NotFound();
            }

            if (model.GetBooking(bookingId) == null)
            {
                return NotFound();
            }

            if (model.GetEventIdOfBooking(bookingId) != model.GetEventId(eventUid))
            {
                return BadRequest();
            }

            if (model.ProcessBooking(bookingId))
            {
                return Ok();
            }
            else
            {
                return Conflict();
            }
            
            
        }

        [HttpPost("bereinigen")]
        public ActionResult<IEnumerable<string>> CleanUpBookingData(LoginPost loginPost)
        {
            if (!auth.CheckIfDebugPermissions(loginPost))
            {
                return Unauthorized();
            }
            model.WipeUnnecessaryData();
            return NoContent();
        }



    }
}
