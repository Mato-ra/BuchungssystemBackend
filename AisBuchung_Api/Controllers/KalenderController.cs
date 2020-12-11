using System;
using System.Web;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore;
using AisBuchung_Api.Models;
using JsonSerializer;

namespace AisBuchung_Api.Controllers
{
    [Route("kalender")]
    [ApiController]
    public class KalenderController : ControllerBase
    {
        private readonly KalenderModel model;
        private readonly AuthModel auth;

        public KalenderController()
        {
            model = new KalenderModel();
            auth = new AuthModel();
        }

        [HttpPost]
        public ActionResult<IEnumerable<string>> PostCalendar(CalendarPost calendarPost)
        {
            var organizerId = auth.GetLoggedInOrganizer(calendarPost);
            if (!auth.CheckIfOrganizerPermissions(organizerId))
            {
                return Unauthorized();
            }

            calendarPost.veranstalter = organizerId;
            var result = model.PostCalendar(calendarPost);
            if (result > 0)
            {
                var path = Json.AddKeyValuePair(Json.CreateNewObject(), "id", result.ToString(), true);
                return Content(path, "application/json");
            }
            else
            {
                return BadRequest();
            }
        }

        [HttpGet("{calendarId}/veranstalter")]
        public ActionResult<IEnumerable<string>> GetCalendarOrganizers(long calendarId, LoginPost loginPost)
        {
            if (!auth.CheckIfOrganizerPermissions(loginPost))
            {
                return Unauthorized();
            }

            var result = model.GetCalendarOrganizers(calendarId);
            if (result != null)
            {
                return Content(result, "application/json");
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet("{calendarId}/veranstaltungen")]
        public ActionResult<IEnumerable<string>> GetEvents(long calendarId, LoginPost loginPost)
        {
            if (!auth.CheckIfOrganizerPermissions(loginPost))
            {
                return Unauthorized();
            }

            var result = new VeranstaltungenModel().GetEvents(calendarId, null);
            if (result != null)
            {
                return Content(result, "application/json");
            }
            else
            {
                return NotFound();
            }
        }

        [HttpDelete("{calendarId}/veranstalter/{organizerId}")]
        public ActionResult<IEnumerable<string>> DeleteCalendarOrganizer(LoginPost loginPost, long calendarId, long organizerId)
        {
            if (!auth.CheckIfCalendarPermissions(loginPost, calendarId))
            {
                return Unauthorized();
            }

            var result = model.GetCalendarOrganizers(calendarId);
            if (result != null)
            {
                if (model.DeleteCalendarOrganizer(calendarId, organizerId))
                {
                    return Ok();
                }
                else
                {
                    return Conflict();
                }
            }
            else
            {
                return NotFound();
            }
        }

        [HttpPost("{calendarId}/veranstalter")]
        public ActionResult<IEnumerable<string>> PostCalendarOrganizer(long calendarId, CalendarPost calendarPost)
        {
            if (!auth.CheckIfCalendarPermissions(calendarPost, calendarId))
            {
                return Unauthorized();
            }

            if (model.GetCalendar(calendarId) == null)
            {
                return NotFound();
            }

            var result = model.PostCalendarOrganizer(calendarId, calendarPost);

            if (result)
            {
                return NoContent();
            }
            else
            {
                return BadRequest();
            }
        }

        [HttpGet]
        public ActionResult<IEnumerable<string>> GetAllCalendars()
        {
            var result = model.GetCalendars();
            if (result != null)
            {
                return Content(result, "application/json");
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet("{calendarId}")]
        public ActionResult<IEnumerable<string>> GetCalendar(long calendarId)
        {
            var result = model.GetCalendar(calendarId);
            if (result != null)
            {
                return Content(result, "application/json");
            }
            else
            {
                return NotFound();
            }
        }

        [HttpPut("{calendarId}")]
        public ActionResult<IEnumerable<string>> PutCalendar(long calendarId, CalendarPost calendarPost)
        {
            if (model.GetCalendar(calendarId) == null)
            {
                return NotFound();
            }

            if (!auth.CheckIfCalendarPermissions(calendarPost, calendarId))
            {
                return Unauthorized();
            }

            if (model.PutCalendar(calendarPost, calendarId))
            {
                return NoContent();
            }
            else
            {
                return Conflict();
            }
        }

        [HttpDelete("{calendarId}")]
        public ActionResult<IEnumerable<string>> DeleteCalendar(LoginPost loginPost, long calendarId)
        {
            if (model.GetCalendar(calendarId) == null)
            {
                return NotFound();
            }

            if (!auth.CheckIfCalendarPermissions(loginPost, calendarId))
            {
                return Unauthorized();
            }

            if (model.DeleteCalendar(calendarId))
            {
                return NoContent();
            }
            else
            {
                return Conflict();
            }
        }

    }
}
