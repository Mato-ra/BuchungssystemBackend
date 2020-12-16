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
    [Route("veranstaltungen")]
    [ApiController]
    public class VeranstaltungenController : ControllerBase
    {
        private readonly VeranstaltungenModel model;
        private readonly AuthModel auth;

        public VeranstaltungenController()
        {
            model = new VeranstaltungenModel();
            auth = new AuthModel();
        }

        [HttpGet]
        public ActionResult<IEnumerable<string>> GetAllEvents(LoginPost loginPost)
        {
            if (!auth.CheckIfOrganizerPermissions(loginPost))
            {
                return Unauthorized();
            }

            var query = Request.QueryString.ToUriComponent();
            query = System.Web.HttpUtility.UrlDecode(query);
            var result = model.GetEvents(query);
            return Content(result, "application/json");
        }

        [HttpGet("{uid}")]
        public ActionResult<IEnumerable<string>> GetEvent(string uid)
        {
            if (model.GetEvent(uid) == null)
            {
                return NotFound();
            }

            var calendarId = Convert.ToInt32(CalendarManager.GetOrganizerCommonName(uid));

            if (new KalenderModel().GetCalendar(calendarId) == null)
            {
                return NotFound();
            }


            var query = Request.QueryString.ToUriComponent();
            query = System.Web.HttpUtility.UrlDecode(query);
            var result = model.GetEvent(uid);
            return Content(result, "application/json");
        }

        [HttpDelete("{uid}")]
        public ActionResult<IEnumerable<string>> DeleteEvent(LoginPost loginPost, string uid)
        {
            if (model.GetEvent(uid) == null)
            {
                return NotFound();
            }

            var calendarId = Convert.ToInt32(CalendarManager.GetOrganizerCommonName(uid));

            if (new KalenderModel().GetCalendar(calendarId) == null)
            {
                return NotFound();
            }

            if (!auth.CheckIfCalendarPermissions(loginPost, calendarId)){
                return Unauthorized();
            }

            if (model.DeleteEvent(calendarId, uid))
            {
                return Ok();
            }
            else
            {
                return NotFound();
            }
        }

        [HttpPut("{uid}")]
        public ActionResult<IEnumerable<string>> PutEvent(EventPost eventPost, string uid)
        {
            if (model.GetEvent(uid) == null)
            {
                return NotFound();
            }

            var calendarId = Convert.ToInt32(CalendarManager.GetOrganizerCommonName(uid));

            if (new KalenderModel().GetCalendar(calendarId) == null)
            {
                return NotFound();
            }

            if (!auth.CheckIfCalendarPermissions(eventPost, calendarId)){
                return Unauthorized();
            }

            var result = model.PutEvent(calendarId, uid, eventPost);
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
