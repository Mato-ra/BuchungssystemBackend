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
        public ActionResult<IEnumerable<string>> GetAllEvents()
        {
            var query = Request.QueryString.ToUriComponent();
            query = System.Web.HttpUtility.UrlDecode(query);
            var result = model.GetEvents(query);
            return Content(result, "application/json");
        }

        [HttpGet("{calendar}")]
        public ActionResult<IEnumerable<string>> GetCalendarEvents(string calendar)
        {
            var calendarId = new KalenderModel().GetCalendarId(calendar);
            if (calendarId == -1)
            {
                return NotFound();
            }

            var query = Request.QueryString.ToUriComponent();
            query = System.Web.HttpUtility.UrlDecode(query);
            var result = model.GetEvents(calendarId, query);
            return Content(result, "application/json");
        }

        [HttpGet("{calendar}/{uid}")]
        public ActionResult<IEnumerable<string>> GetEvent(string calendar, string uid)
        {
            var calendarId = new KalenderModel().GetCalendarId(calendar);
            if (calendarId == -1)
            {
                return NotFound();
            }

            //TODO Fix

            var query = Request.QueryString.ToUriComponent();
            query = System.Web.HttpUtility.UrlDecode(query);
            var result = model.GetEvent(uid);
            return Content(result, "application/json");
        }

        [HttpDelete("{calendar}/{uid}")]
        public ActionResult<IEnumerable<string>> DeleteEvent(LoginPost loginPost, string calendar, string uid)
        {
            var calendarId = new KalenderModel().GetCalendarId(calendar);
            if (calendarId == -1)
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

        [HttpPost("{calendar}")]
        public ActionResult<IEnumerable<string>> PostEvent(EventPost eventPost, string calendar)
        {
            var calendarId = new KalenderModel().GetCalendarId(calendar);
            if (calendarId == -1)
            {
                return NotFound();
            }

            if (!auth.CheckIfCalendarPermissions(eventPost, calendarId)){
                return Unauthorized();
            }

            var result = model.PostEvent(calendarId, eventPost);
            
            if (result != null)
            {
                var path = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.Value}/veranstaltungen/{calendar}/{result}";
                return Created(path, null);
            }
            else
            {
                return BadRequest();
            }
        }

        [HttpPut("{calendar}/{uid}")]
        public ActionResult<IEnumerable<string>> PutEvent(EventPost eventPost, string calendar, string uid)
        {
            var calendarId = new KalenderModel().GetCalendarId(calendar);
            if (calendarId == -1)
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
