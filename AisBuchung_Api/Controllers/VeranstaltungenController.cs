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

        [HttpGet("{calendarId}")]
        public ActionResult<IEnumerable<string>> GetCalendarEvents(long calendarId)
        {
            if (calendarId == -1)
            {
                return NotFound();
            }

            var query = Request.QueryString.ToUriComponent();
            query = System.Web.HttpUtility.UrlDecode(query);
            var result = model.GetEvents(calendarId, query);
            return Content(result, "application/json");
        }

        [HttpGet("{calendarId}/{uid}")]
        public ActionResult<IEnumerable<string>> GetEvent(long calendarId, string uid)
        {
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

        [HttpDelete("{calendarId}/{uid}")]
        public ActionResult<IEnumerable<string>> DeleteEvent(LoginPost loginPost, long calendarId, string uid)
        {
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

        [HttpPost("{calendarId}")]
        public ActionResult<IEnumerable<string>> PostEvent(EventPost eventPost, long calendarId)
        {
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
                result = Json.AddKeyValuePair(Json.CreateNewObject(), "uid", result, true);
                return Content(result, "application/json");
            }
            else
            {
                return BadRequest();
            }
        }

        [HttpPut("{calendarId}/{uid}")]
        public ActionResult<IEnumerable<string>> PutEvent(EventPost eventPost, long calendarId, string uid)
        {
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
