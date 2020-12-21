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
    [Route("veranstalter")]
    [ApiController]
    public class VeranstalterController : ControllerBase
    {
        private readonly VeranstalterModel model;
        private readonly AuthModel auth;

        public VeranstalterController()
        {
            model = new VeranstalterModel();
            auth = new AuthModel();
        }

        [HttpGet]
        public ActionResult<IEnumerable<string>> GetAllOrganizers(LoginPost loginPost)
        {
            if (!auth.CheckIfOrganizerPermissions(loginPost))
            {
                return Unauthorized();
            }

            var query = Request.QueryString.ToUriComponent();
            query = System.Web.HttpUtility.UrlDecode(query);
            var result = model.GetOrganizers(query);
            return Content(result, "application/json");
        }

        [HttpGet("{id}")]
        public ActionResult<IEnumerable<string>> GetOrganizer(LoginPost loginPost, long id)
        {
            if (!auth.CheckIfOrganizerPermissions(loginPost))
            {
                return Unauthorized();
            }

            var result = model.GetOrganizer(id);
            if (result == null)
            {
                return NotFound();
            }
            return Content(result, "application/json");
        }

        [HttpGet("{id}/kalender")]
        public ActionResult<IEnumerable<string>> GetOrganizerCalendars(LoginPost loginPost, long id)
        {
            if (!auth.CheckIfOrganizerPermissions(loginPost))
            {
                return Unauthorized();
            }

            var result = model.GetOrganizerCalendars(id);
            if (result == null)
            {
                return NotFound();
            }

            return Content(result, "application/json");
        }

        [HttpPost]
        public ActionResult<IEnumerable<string>> PostOrganizer(OrganizerPost organizerPost)
        {
            var validation = new DataValidation();
            var errorMessage = String.Empty;
            if (!validation.CheckIfPasswordIsValid(organizerPost.passwort, out errorMessage))
            {
                return auth.CreateErrorMessageResponse(errorMessage);
            };

            var result = model.PostOrganizer(organizerPost, out errorMessage);
            if (result > 0)
            {
                return Ok();
            }
            else
            {
                return auth.CreateErrorMessageResponse(errorMessage, 400);
            }
        }

        [HttpPost("anlegen")]
        public ActionResult<IEnumerable<string>> PostNewOrganizer(OrganizerPost organizerPost)
        {
            if (!auth.CheckIfAdminPermissions(organizerPost))
            {
                return Unauthorized();
            }

            var validation = new DataValidation();
            var errorMessage = String.Empty;
            if (!validation.CheckIfPasswordIsValid(organizerPost.passwort, out errorMessage))
            {
                return auth.CreateErrorMessageResponse(errorMessage);
            };

            var result = model.PostAuthorizedOrganizer(organizerPost, out errorMessage);
            if (result > 0)
            {
                return Ok();
            }
            else
            {
                return auth.CreateErrorMessageResponse(errorMessage, 400);
            }
        }

        [HttpPost("{id}/passwort")]
        public ActionResult<IEnumerable<string>> PostPassword(long id, PasswordPost passwordPost)
        {
            var errorMessage = String.Empty;

            if (!auth.CheckIfOrganizerPermissions(passwordPost))
            {
                return Unauthorized();
            }

            var email = model.GetOrganizerEmail(id);
            if (auth.CheckIfAdminPermissions(passwordPost))
            {
                if (!model.PostPassword(id, passwordPost, out errorMessage))
                {
                    return auth.CreateErrorMessageResponse(errorMessage);
                }
                else
                {
                    return NoContent();
                }
            }

            if (auth.CheckIfOrganizerPermissions(passwordPost, id))
            {
                if (auth.GetLoggedInOrganizer(Json.DeserializeString(email), passwordPost.HashPassword(passwordPost.altesPasswort)) != id)
                {
                    errorMessage = "Das alte Passwort ist nicht korrekt.";
                    return auth.CreateErrorMessageResponse(errorMessage);
                }
            }
            else
            {
                return Unauthorized();
            }
            
            if (!model.PostPassword(id, passwordPost, out errorMessage))
            {
                return auth.CreateErrorMessageResponse(errorMessage);
            }

            return NoContent();
        }

        [HttpPost("{id}/email")]
        public ActionResult<IEnumerable<string>> ChangeEmail(EmailPost emailPost, long id)
        {
            if (auth.CheckIfAdminPermissions(emailPost))
            {
                if (model.GetOrganizer(emailPost.neueEmail) != null)
                {
                    return BadRequest();
                }

                if (model.ChangeEmail(id, emailPost.neueEmail))
                {
                    return NoContent();
                }
                else
                {
                    return BadRequest();
                }
            }

            if (!auth.CheckIfOrganizerPermissions(emailPost, id))
            {
                return Unauthorized();
            }

            if (model.GetOrganizer(emailPost.neueEmail) != null)
            {
                return BadRequest();
            }
            if (model.PostEmail(id, emailPost))
            {
                return NoContent();
            }
            else
            {
                return BadRequest();
            }
        }

        [HttpPut("{id}")]
        public ActionResult<IEnumerable<string>> PutOrganizer(long id, OrganizerPost organizerPost)
        {
            //TODO OrganizerPost überprüfen
            if (!auth.CheckIfOrganizerPermissions(organizerPost, id))
            {
                return Unauthorized();
            }

            var result = model.PutOrganizer(id, organizerPost);
            if (result)
            {
                return NoContent();
            }
            else
            {
                return NotFound();
            }
        }

        [HttpDelete("{id}")]
        public ActionResult<IEnumerable<string>> DeleteOrganizer(LoginPost loginPost, long id)
        {
            if (!auth.CheckIfOrganizerPermissions(loginPost, id))
            {
                return Unauthorized();
            }

            var result = model.DeleteOrganizer(id);
            if (result)
            {
                return NoContent();
            }
            else
            {
                return NotFound();
            }
        }

        [HttpPost("{id}/autorisieren")]
        public ActionResult<IEnumerable<string>> AuthorizeOrganizer(LoginPost loginPost, long id)
        {
            if (!auth.CheckIfOrganizerPermissions(loginPost))
            {
                return Unauthorized();
            }

            

            if (model.AuthorizeOrganizer(id))
            {
                return NoContent();
            }
            else
            {
                if (auth.CheckIfAdminPermissions(loginPost) && ConfigManager.CheckIfAdminsCanVerify())
                {
                    if (model.AuthorizeOrganizer(id))
                    {
                        return NoContent();
                    }
                    else
                    {
                        return BadRequest();
                    }
                }

                return BadRequest();
            }
        }
    }
    
    
}
